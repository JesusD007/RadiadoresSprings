using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationApp.BackgroundServices;

/// <summary>
/// Servicio de fondo que reproduce las operaciones offline pendientes contra el Core
/// en cuanto este recupera disponibilidad.
///
/// DISEÑO:
/// — Se activa inmediatamente vía el evento CoreRecuperado del CircuitBreakerStateService.
/// — También ejecuta un ciclo periódico (fallback) para reintentar operaciones que
///   quedaron en estado "Pendiente" con < MaxIntentos intentos.
/// — Semáforo interno evita ejecuciones concurrentes.
/// — Idempotency-Key garantiza que los reintentos no generen duplicados en Core.
/// — Errores 4xx son definitivos (marcados "Rechazada" inmediatamente).
/// — Errores 5xx / red se reintentan hasta MaxIntentos=3.
/// — Después de cada ronda de sync, fuerza una re-sincronización de mirrors para
///   que los datos locales queden actualizados con el estado definitivo del Core.
/// </summary>
public class OperacionPendienteSyncService : BackgroundService
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ICoreTokenService _coreTokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OperacionPendienteSyncService> _logger;

    // Evita dos rondas de sync simultáneas (p.ej. evento + timer al mismo tiempo)
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private const int MaxIntentos = 3;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public OperacionPendienteSyncService(
        ICircuitBreakerStateService cbState,
        ICoreTokenService coreTokenService,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<OperacionPendienteSyncService> logger)
    {
        _cbState = cbState;
        _coreTokenService = coreTokenService;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;

        // Suscribirse al evento de recuperación del Core para sync inmediata
        _cbState.CoreRecuperado += OnCoreRecuperado;
    }

    // ── Evento: Core acaba de recuperarse ─────────────────────────────────────

    private void OnCoreRecuperado(object? sender, EventArgs e)
    {
        _logger.LogInformation("[OpSync] Evento CoreRecuperado recibido — iniciando sync inmediata");
        // Task.Run porque el evento puede dispararse desde un hilo de fondo (CircuitBreakerStateService)
        _ = Task.Run(() => EjecutarRondaAsync(CancellationToken.None));
    }

    // ── Loop periódico (fallback para reintentos) ──────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay inicial: esperar a que CoreHealthCheckService haga su primer check
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        var intervalMinutes = _config.GetValue<int>("Sync:RetryIntervalMinutes", 30);
        _logger.LogInformation("[OpSync] Iniciando — ciclo de reintentos cada {Min} min", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);

                if (_cbState.CoreAvailable)
                {
                    _logger.LogDebug("[OpSync] Ciclo periódico — verificando operaciones pendientes");
                    await EjecutarRondaAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* shutdown normal */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpSync] Error inesperado en loop periódico");
            }
        }

        // Desuscribir al detenerse el servicio
        _cbState.CoreRecuperado -= OnCoreRecuperado;
    }

    // ── Orchestración de la ronda de sync ─────────────────────────────────────

    private async Task EjecutarRondaAsync(CancellationToken ct)
    {
        // Si ya hay una ronda en curso, no iniciar otra
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogDebug("[OpSync] Sync ya en progreso — omitiendo");
            return;
        }

        try
        {
            _logger.LogInformation("[OpSync] === Iniciando ronda de sincronización ===");

            // Obtener token M2M una vez para toda la ronda
            string token;
            try
            {
                token = await _coreTokenService.GetTokenAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpSync] No se pudo obtener token M2M — abortando ronda");
                return;
            }

            var client = _httpClientFactory.CreateClient("CoreApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await SyncOperacionesPendientesAsync(client, ct);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    // ── Procesamiento de OperacionPendiente ───────────────────────────────────

    private async Task SyncOperacionesPendientesAsync(HttpClient client, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        // Cargar todas las pendientes con reintentos disponibles, en orden cronológico
        var pendientes = await db.OperacionesPendientes
            .Where(op => op.Estado == "Pendiente" && op.IntentosSync < MaxIntentos)
            .OrderBy(op => op.FechaCreacion)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
        {
            _logger.LogInformation("[OpSync] No hay operaciones pendientes");
            return;
        }

        _logger.LogInformation("[OpSync] Procesando {Count} operaciones pendientes", pendientes.Count);

        int sincronizadas = 0, rechazadas = 0, reintentables = 0;

        foreach (var op in pendientes)
        {
            if (ct.IsCancellationRequested) break;

            // Si el Core cae durante la ronda, pausar para no acumular errores
            if (!_cbState.CoreAvailable)
            {
                _logger.LogWarning("[OpSync] Core dejó de estar disponible — pausando sync");
                break;
            }

            op.IntentosSync++;
            op.UltimoIntento = DateTimeOffset.UtcNow;

            try
            {
                var resultado = await EnviarOperacionAsync(client, op, ct);

                if (resultado.Exitoso)
                {
                    op.Estado = "Sincronizada";
                    op.RespuestaCore = resultado.RespuestaBody;
                    sincronizadas++;

                    _logger.LogInformation(
                        "[OpSync] ✓ Op {Id} ({Tipo}/{Operacion}) sincronizada — HTTP {Status}",
                        op.Id, op.TipoEntidad, op.TipoOperacion, resultado.HttpStatus);

                    // Post-procesamiento: actualizar entidades derivadas
                    await PostProcesarSyncAsync(db, op, resultado.RespuestaBody, ct);
                }
                else if (resultado.EsErrorDefinitivo)
                {
                    // 4xx: el Core rechaza la operación permanentemente
                    op.Estado = "Rechazada";
                    op.RespuestaCore = resultado.RespuestaBody;
                    op.MotivoRechazo = $"HTTP {resultado.HttpStatus}: {Truncar(resultado.RespuestaBody, 500)}";
                    rechazadas++;

                    _logger.LogWarning(
                        "[OpSync] ✗ Op {Id} rechazada definitivamente — HTTP {Status}: {Motivo}",
                        op.Id, resultado.HttpStatus, Truncar(resultado.RespuestaBody, 200));
                }
                else
                {
                    // 5xx / timeout: reintentable
                    if (op.IntentosSync >= MaxIntentos)
                    {
                        op.Estado = "Rechazada";
                        op.MotivoRechazo = $"Máx {MaxIntentos} reintentos. Último HTTP {resultado.HttpStatus}: {Truncar(resultado.RespuestaBody, 300)}";
                        rechazadas++;
                        _logger.LogError("[OpSync] ✗ Op {Id} agotó reintentos — marcada como Rechazada", op.Id);
                    }
                    else
                    {
                        reintentables++;
                        _logger.LogWarning(
                            "[OpSync] ⚠ Op {Id} fallo temporal HTTP {Status} — intento {N}/{Max}",
                            op.Id, resultado.HttpStatus, op.IntentosSync, MaxIntentos);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpSync] Excepción al procesar op {Id}", op.Id);

                if (op.IntentosSync >= MaxIntentos)
                {
                    op.Estado = "Rechazada";
                    op.MotivoRechazo = $"Excepción tras {MaxIntentos} intentos: {Truncar(ex.Message, 500)}";
                    rechazadas++;
                }
                else
                {
                    reintentables++;
                }
            }

            // Persistir el estado de esta operación antes de continuar con la siguiente
            // (un fallo posterior no revierte operaciones ya sincronizadas)
            try { await db.SaveChangesAsync(ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpSync] Error al guardar estado de op {Id}", op.Id);
            }
        }

        _logger.LogInformation(
            "[OpSync] === Ronda completada — Sincronizadas: {S}, Rechazadas: {R}, Pendientes: {P} ===",
            sincronizadas, rechazadas, reintentables);
    }

    // ── Envío HTTP individual ──────────────────────────────────────────────────

    private static async Task<OperacionResult> EnviarOperacionAsync(
        HttpClient client, OperacionPendiente op, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(new HttpMethod(op.MetodoHttp), op.EndpointCore);

        // Añadir Idempotency-Key para que Core descarte duplicados en reintentos
        req.Headers.TryAddWithoutValidation("Idempotency-Key", op.IdempotencyKey.ToString());

        if (op.MetodoHttp != "DELETE" && !string.IsNullOrEmpty(op.PayloadJson))
            req.Content = new StringContent(op.PayloadJson, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(req, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        int status = (int)response.StatusCode;

        return new OperacionResult(
            Exitoso: response.IsSuccessStatusCode,
            EsErrorDefinitivo: status >= 400 && status < 500,
            HttpStatus: status,
            RespuestaBody: body);
    }

    // ── Post-procesamiento de operaciones exitosas ────────────────────────────

    private async Task PostProcesarSyncAsync(
        IntegrationDbContext db, OperacionPendiente op, string? respBody, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(respBody)) return;

        try
        {
            // Cuando Core devuelve el ID definitivo de una sesión de caja → actualizar SesionCajaMirror
            if (op.TipoEntidad == "Caja" && op.TipoOperacion == "AbrirSesion"
                && op.IdLocalTemporal is not null
                && Guid.TryParse(op.IdLocalTemporal, out var idLocal))
            {
                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("sesionCajaId", out var sesionIdEl)
                    && sesionIdEl.TryGetInt32(out var coreSesionId))
                {
                    var sesion = await db.SesionesCajaMirror
                        .FirstOrDefaultAsync(s => s.IdLocal == idLocal, ct);

                    if (sesion is not null)
                    {
                        sesion.CoreSesionId = coreSesionId;
                        sesion.EstadoSync = "Sincronizada";
                        _logger.LogInformation(
                            "[OpSync] SesionCajaMirror {LocalId} → CoreSesionId={CoreId}",
                            idLocal, coreSesionId);
                    }
                }
            }

            // Cuando Core devuelve el CoreId de un cliente creado offline → actualizar ClienteMirror
            if (op.TipoEntidad == "Cliente" && op.TipoOperacion == "Crear"
                && op.IdLocalTemporal is not null
                && Guid.TryParse(op.IdLocalTemporal, out var clienteLocalId))
            {
                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("id", out var coreIdEl)
                    && coreIdEl.TryGetInt32(out var coreClienteId))
                {
                    var cliente = await db.ClientesMirror
                        .FirstOrDefaultAsync(c => c.LocalId == clienteLocalId, ct);

                    if (cliente is not null)
                    {
                        cliente.CoreId = coreClienteId;
                        cliente.EsLocal = false;
                        cliente.UltimaSync = DateTime.UtcNow;
                        _logger.LogInformation(
                            "[OpSync] ClienteMirror {LocalId} → CoreId={CoreId}",
                            clienteLocalId, coreClienteId);
                    }
                }
            }

            // Cuando Core cierra la sesión de caja definitivamente
            if (op.TipoEntidad == "Caja" && op.TipoOperacion == "CerrarSesion"
                && op.IdLocalTemporal is not null
                && Guid.TryParse(op.IdLocalTemporal, out var sesionLocalId))
            {
                var sesion = await db.SesionesCajaMirror
                    .FirstOrDefaultAsync(s => s.IdLocal == sesionLocalId, ct);

                if (sesion is not null)
                {
                    sesion.Estado = "Cerrada";
                    sesion.EstadoSync = "Sincronizada";
                }
            }
        }
        catch (Exception ex)
        {
            // El post-procesamiento es best-effort; no debe abortar el sync
            _logger.LogWarning(ex, "[OpSync] Error en post-procesamiento de op {Id}", op.Id);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Truncar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty
        : s.Length <= max ? s
        : s[..max] + "…";

    private record OperacionResult(
        bool Exitoso,
        bool EsErrorDefinitivo,
        int HttpStatus,
        string RespuestaBody);
}
