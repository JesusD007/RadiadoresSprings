using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using IntegrationApp.Helpers;

namespace IntegrationApp.BackgroundServices;

/// <summary>
/// Sincroniza los mirrors locales con el catálogo completo del Core cada N minutos.
/// Solo opera cuando CoreAvailable == true.
///
/// Tablas sincronizadas:
/// — ProductoMirror  → catálogo de productos e inventario
/// — UsuarioMirror   → credenciales y roles para autenticación offline
/// — ClienteMirror   → datos de clientes para operaciones offline
///
/// INVARIANTE: los mirrors son réplicas de lectura del Core.
/// Cuando Core está activo, las escrituras van siempre al Core.
/// Los mirrors solo se usan cuando Core está caído (modo offline).
/// </summary>
public class MirrorSyncService : BackgroundService
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ICoreTokenService _coreTokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MirrorSyncService> _logger;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public MirrorSyncService(
        ICircuitBreakerStateService cbState,
        ICoreTokenService coreTokenService,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<MirrorSyncService> logger)
    {
        _cbState = cbState;
        _coreTokenService = coreTokenService;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;

        // Sincronizar inmediatamente cuando el Core se recupera
        _cbState.CoreRecuperado += OnCoreRecuperado;
    }

    private void OnCoreRecuperado(object? sender, EventArgs e)
    {
        _logger.LogInformation("[MirrorSync] CoreRecuperado — iniciando re-sincronización de mirrors");
        _ = Task.Run(() => SyncAllAsync(CancellationToken.None));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int>("Mirror:SyncIntervalMinutes", 5);
        _logger.LogInformation("[MirrorSync] Iniciando — sync cada {Interval} min", intervalMinutes);

        // Delay inicial: dar tiempo al CoreHealthCheckService de hacer su primer ping
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_cbState.CoreAvailable)
                    await SyncAllAsync(stoppingToken);
                else
                    _logger.LogDebug("[MirrorSync] Core no disponible — omitiendo ciclo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MirrorSync] Error inesperado en ciclo de sync");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _cbState.CoreRecuperado -= OnCoreRecuperado;
    }

    // ── Orquestación de todos los mirrors ─────────────────────────────────────

    private async Task SyncAllAsync(CancellationToken ct)
    {
        try
        {
            var token = await _coreTokenService.GetTokenAsync(ct);
            var client = _httpClientFactory.CreateClient("CoreApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            await SyncProductosAsync(client, ct);
            await SyncUsuariosAsync(client, ct);
            await SyncClientesAsync(client, ct);
            await SyncSucursalesAsync(client, ct);
            await SyncCategoriasAsync(client, ct);
            await SyncCajasAsync(client, ct);
            await SyncCuentasCobrarAsync(client, ct);

            _cbState.UpdateLastSync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MirrorSync] Error al obtener token o sincronizar mirrors");
        }
    }

    // ── ProductoMirror ─────────────────────────────────────────────────────────

    private async Task SyncProductosAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando ProductoMirror...");
        try
        {
            int page = 1;
            const int pageSize = 100;
            int total = 0;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            while (true)
            {
                var response = await client.GetAsync(
                    $"/api/v1/productos?pagina={page}&tamano={pageSize}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MirrorSync] ProductoMirror página {P}: HTTP {S}",
                        page, (int)response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var result = ProxyHelper.Unwrap<ProductoPagedResultRaw>(content, _json);
                if (result?.Items == null || result.Items.Count == 0) break;

                foreach (var item in result.Items)
                {
                    var existing = await db.ProductosMirror.FindAsync([item.Id], ct);
                    if (existing is null)
                        db.ProductosMirror.Add(MapProducto(item));
                    else
                        AplicarProducto(existing, item);
                    total++;
                }

                await db.SaveChangesAsync(ct);
                if (page * pageSize >= result.Total) break;
                page++;
            }

            _logger.LogInformation("[MirrorSync] ProductoMirror: {Count} registros sincronizados", total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MirrorSync] Error sincronizando ProductoMirror");
        }
    }

    // ── UsuarioMirror ──────────────────────────────────────────────────────────

    private async Task SyncUsuariosAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando UsuarioMirror...");
        try
        {
            // Core expone GET /api/v1/usuarios/mirror — devuelve todos los usuarios activos
            // con sus password hashes (BCrypt) para uso offline.
            // Este endpoint debe estar protegido y solo accesible para servicios M2M.
            var response = await client.GetAsync("/api/v1/usuarios/mirror", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[MirrorSync] UsuarioMirror: HTTP {S}", (int)response.StatusCode);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var usuarios = ProxyHelper.Unwrap<List<UsuarioMirrorRaw>>(content, _json);
            if (usuarios is null || usuarios.Count == 0)
            {
                _logger.LogInformation("[MirrorSync] UsuarioMirror: sin registros del Core");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            foreach (var u in usuarios)
            {
                var existing = await db.UsuariosMirror.FindAsync([u.Id], ct);
                if (existing is null)
                {
                    db.UsuariosMirror.Add(new UsuarioMirror
                    {
                        Id           = u.Id,
                        Username     = u.Username,
                        PasswordHash = u.PasswordHash,
                        Rol          = u.Rol,
                        Nombre       = u.Nombre,
                        Apellido     = u.Apellido ?? string.Empty,
                        Email        = u.Email,
                        EsActivo     = u.EsActivo,
                        UltimaSync   = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Username     = u.Username;
                    existing.PasswordHash = u.PasswordHash;
                    existing.Rol          = u.Rol;
                    existing.Nombre       = u.Nombre;
                    existing.Apellido     = u.Apellido ?? string.Empty;
                    existing.Email        = u.Email;
                    existing.EsActivo     = u.EsActivo;
                    existing.UltimaSync   = DateTime.UtcNow;
                }
            }

            // Desactivar usuarios que ya no existen en Core
            var coreIds = usuarios.Select(u => u.Id).ToHashSet();
            var obsoletos = await db.UsuariosMirror
                .Where(u => !coreIds.Contains(u.Id))
                .ToListAsync(ct);
            foreach (var o in obsoletos)
                o.EsActivo = false;

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[MirrorSync] UsuarioMirror: {Count} registros sincronizados", usuarios.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MirrorSync] Error sincronizando UsuarioMirror");
        }
    }

    // ── ClienteMirror ──────────────────────────────────────────────────────────

    private async Task SyncClientesAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando ClienteMirror...");
        try
        {
            int page = 1;
            const int pageSize = 200;
            int total = 0;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            while (true)
            {
                var response = await client.GetAsync(
                    $"/api/v1/clientes?pagina={page}&tamano={pageSize}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[MirrorSync] ClienteMirror página {P}: HTTP {S}",
                        page, (int)response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var result = ProxyHelper.Unwrap<ClientePagedResultRaw>(content, _json);
                if (result?.Items == null || result.Items.Count == 0) break;

                foreach (var item in result.Items)
                {
                    // Buscar por CoreId (el cliente ya existía antes)
                    var existing = await db.ClientesMirror
                        .FirstOrDefaultAsync(c => c.CoreId == item.Id, ct);

                    if (existing is null)
                    {
                        db.ClientesMirror.Add(new ClienteMirror
                        {
                            LocalId        = item.Id,
                            CoreId         = item.Id,
                            Nombre         = item.Nombre,
                            Apellido       = item.Apellido,
                            Email          = item.Email,
                            Telefono       = item.Telefono,
                            Direccion      = item.Direccion,
                            RFC            = item.RFC,
                            Tipo           = item.Tipo ?? "Regular",
                            LimiteCredito  = item.LimiteCredito,
                            SaldoPendiente = item.SaldoPendiente,
                            EsActivo       = item.EsActivo,
                            EsLocal        = false,
                            UltimaSync     = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.Nombre         = item.Nombre;
                        existing.Apellido       = item.Apellido;
                        existing.Email          = item.Email;
                        existing.Telefono       = item.Telefono;
                        existing.Direccion      = item.Direccion;
                        existing.RFC            = item.RFC;
                        existing.Tipo           = item.Tipo ?? "Regular";
                        existing.LimiteCredito  = item.LimiteCredito;
                        existing.SaldoPendiente = item.SaldoPendiente;
                        existing.EsActivo       = item.EsActivo;
                        existing.EsLocal        = false;
                        existing.UltimaSync     = DateTime.UtcNow;
                    }

                    total++;
                }

                await db.SaveChangesAsync(ct);
                if (page * pageSize >= result.Total) break;
                page++;
            }

            _logger.LogInformation("[MirrorSync] ClienteMirror: {Count} registros sincronizados", total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MirrorSync] Error sincronizando ClienteMirror");
        }
    }


    // ── SucursalMirror ─────────────────────────────────────────────────────────

    private async Task SyncSucursalesAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando SucursalMirror...");
        try
        {
            var response = await client.GetAsync("/api/v1/sucursales", ct);
            if (!response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync(ct);
            var sucursales = ProxyHelper.Unwrap<List<SucursalItemRaw>>(content, _json);
            if (sucursales == null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            foreach (var item in sucursales)
            {
                var existing = await db.SucursalesMirror.FindAsync([item.Id], ct);
                if (existing is null)
                {
                    db.SucursalesMirror.Add(new SucursalMirror
                    {
                        CoreId = item.Id, Nombre = item.Nombre,
                        Direccion = item.Direccion, Telefono = item.Telefono,
                        EsActivo = item.EsActiva, UltimaSync = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Nombre = item.Nombre; existing.Direccion = item.Direccion;
                    existing.Telefono = item.Telefono; existing.EsActivo = item.EsActiva;
                    existing.UltimaSync = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[MirrorSync] SucursalMirror: {C} registros", sucursales.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "[MirrorSync] Error SucursalMirror"); }
    }

    // ── CategoriaMirror ────────────────────────────────────────────────────────

    private async Task SyncCategoriasAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando CategoriaMirror...");
        try
        {
            var response = await client.GetAsync("/api/v1/categorias", ct);
            if (!response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync(ct);
            var categorias = ProxyHelper.Unwrap<List<CategoriaItemRaw>>(content, _json);
            if (categorias == null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            foreach (var item in categorias)
            {
                var existing = await db.CategoriasMirror.FindAsync([item.Id], ct);
                if (existing is null)
                {
                    db.CategoriasMirror.Add(new CategoriaMirror
                    {
                        CoreId = item.Id, Nombre = item.Nombre,
                        Descripcion = item.Descripcion, EsActivo = item.EsActiva,
                        UltimaSync = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Nombre = item.Nombre; existing.Descripcion = item.Descripcion;
                    existing.EsActivo = item.EsActiva; existing.UltimaSync = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[MirrorSync] CategoriaMirror: {C} registros", categorias.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "[MirrorSync] Error CategoriaMirror"); }
    }

    // ── CajaMirror ─────────────────────────────────────────────────────────────

    private async Task SyncCajasAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando CajaMirror...");
        try
        {
            var response = await client.GetAsync("/api/v1/caja", ct);
            if (!response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync(ct);
            var cajas = ProxyHelper.Unwrap<List<CajaItemRaw>>(content, _json);
            if (cajas == null) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            foreach (var item in cajas)
            {
                var existing = await db.CajasMirror.FindAsync([item.Id], ct);
                if (existing is null)
                {
                    db.CajasMirror.Add(new CajaMirror
                    {
                        CoreId = item.Id, Nombre = item.Nombre,
                        SucursalId = item.SucursalId, EsActiva = item.EsActiva,
                        UltimaSync = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Nombre = item.Nombre; existing.SucursalId = item.SucursalId;
                    existing.EsActiva = item.EsActiva; existing.UltimaSync = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[MirrorSync] CajaMirror: {C} registros", cajas.Count);
        }
        catch (Exception ex) { _logger.LogError(ex, "[MirrorSync] Error CajaMirror"); }
    }

    // ── CuentaCobrarMirror ─────────────────────────────────────────────────────

    private async Task SyncCuentasCobrarAsync(HttpClient client, CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Sincronizando CuentaCobrarMirror...");
        try
        {
            int page = 1;
            const int pageSize = 100;
            int total = 0;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            while (true)
            {
                // Solo traemos cuentas Pendientes o Vencidas (no Canceladas ni Pagadas, para ahorrar espacio)
                var response = await client.GetAsync(
                    $"/api/v1/cuentas-cobrar?pagina={page}&tamano={pageSize}&estado=Pendiente", ct);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct);
                var result = ProxyHelper.Unwrap<CuentaCobrarPagedResultRaw>(content, _json);
                if (result?.Items == null || result.Items.Count == 0) break;

                foreach (var item in result.Items)
                {
                    // Convertir DateTimeOffset → DateTime UTC para la entidad
                    var fechaUtc = item.FechaVencimiento.UtcDateTime;

                    var existing = await db.CuentasCobrarMirror.FindAsync([item.Id], ct);
                    if (existing is null)
                    {
                        db.CuentasCobrarMirror.Add(new CuentaCobrarMirror
                        {
                            CoreId = item.Id, VentaId = item.VentaId, ClienteId = item.ClienteId,
                            MontoTotal = item.MontoTotal, SaldoPendiente = item.SaldoPendiente,
                            FechaVencimiento = fechaUtc, Estado = item.Estado,
                            UltimaSync = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.VentaId = item.VentaId; existing.ClienteId = item.ClienteId;
                        existing.MontoTotal = item.MontoTotal; existing.SaldoPendiente = item.SaldoPendiente;
                        existing.FechaVencimiento = fechaUtc; existing.Estado = item.Estado;
                        existing.UltimaSync = DateTime.UtcNow;
                    }
                    total++;
                }

                await db.SaveChangesAsync(ct);
                if (page * pageSize >= result.Total) break;
                page++;
            }

            _logger.LogInformation("[MirrorSync] CuentaCobrarMirror: {Count} registros", total);
        }
        catch (Exception ex) { _logger.LogError(ex, "[MirrorSync] Error CuentaCobrarMirror"); }
    }

    // ── Mappers ProductoMirror ────────────────────────────────────────────────

    private static ProductoMirror MapProducto(ProductoItemRaw item) => new()
    {
        Id         = item.Id,
        Codigo     = item.Codigo,
        Nombre     = item.Nombre,
        Precio     = item.Precio,
        Stock      = item.Stock,
        EsActivo   = item.EsActivo,
        Categoria  = item.Categoria,
        UltimaSync = DateTime.UtcNow
    };

    private static void AplicarProducto(ProductoMirror existing, ProductoItemRaw item)
    {
        existing.Codigo    = item.Codigo;
        existing.Nombre    = item.Nombre;
        existing.Precio    = item.Precio;
        existing.Stock     = item.Stock;
        existing.EsActivo  = item.EsActivo;
        existing.Categoria = item.Categoria;
        existing.UltimaSync = DateTime.UtcNow;
    }

    // ── DTOs internos de deserialización ──────────────────────────────────────

    private record ProductoPagedResultRaw(List<ProductoItemRaw>? Items, int Total, int Page, int PageSize);
    private record ProductoItemRaw(int Id, string Codigo, string Nombre, decimal Precio, int Stock, string Categoria, bool EsActivo);

    private record UsuarioMirrorRaw(int Id, string Username, string PasswordHash, string Rol,
        string Nombre, string? Apellido, string? Email, bool EsActivo);

    private record ClientePagedResultRaw(List<ClienteItemRaw>? Items, int Total, int Page, int PageSize);
    private record ClienteItemRaw(int Id, string Nombre, string? Apellido, string? Email,
        string? Telefono, string? Direccion, string? RFC, string? Tipo,
        decimal LimiteCredito, decimal SaldoPendiente, bool EsActivo);

    private record SucursalItemRaw(int Id, string Nombre, string? Direccion, string? Telefono, bool EsActiva);
    private record CategoriaItemRaw(int Id, string Nombre, string? Descripcion, int TotalProductos, bool EsActiva);
    private record CajaItemRaw(int Id, int SucursalId, string NombreSucursal, string Numero, string Nombre, bool EsActiva);
    
    private record CuentaCobrarPagedResultRaw(List<CuentaCobrarItemRaw>? Items, int Total, int Page, int PageSize);
    // FechaVencimiento como DateTimeOffset para capturar correctamente el offset
    // del Core. Si el JSON no incluye offset (e.g. "2026-05-15T00:00:00"),
    // DateTimeOffset lo interpreta con offset 0 (UTC), evitando Kind=Unspecified.
    private record CuentaCobrarItemRaw(int Id, int VentaId, int ClienteId, decimal MontoTotal,
        decimal SaldoPendiente, DateTimeOffset FechaVencimiento, string Estado);
}
