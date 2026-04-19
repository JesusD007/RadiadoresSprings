using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.BackgroundServices;

/// <summary>
/// Sincroniza ProductoMirror con el catálogo completo del Core cada N minutos.
/// Solo sincroniza cuando CoreAvailable == true.
/// En modo offline, todos los endpoints de productos sirven desde ProductoMirror.
/// </summary>
public class MirrorSyncService : BackgroundService
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MirrorSyncService> _logger;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public MirrorSyncService(
        ICircuitBreakerStateService cbState,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<MirrorSyncService> logger)
    {
        _cbState = cbState;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int>("Mirror:SyncIntervalMinutes", 5);
        _logger.LogInformation("[MirrorSync] Iniciando sincronización de inventario cada {Interval} min", intervalMinutes);

        // Pequeño delay inicial para que el health check tenga tiempo de correr primero
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_cbState.CoreAvailable)
            {
                await SyncProductosAsync(stoppingToken);
            }
            else
            {
                _logger.LogDebug("[MirrorSync] Core no disponible — omitiendo sincronización");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task SyncProductosAsync(CancellationToken ct)
    {
        _logger.LogInformation("[MirrorSync] Iniciando sync de ProductoMirror...");
        try
        {
            var client = _httpClientFactory.CreateClient("CoreApi");
            int page = 1;
            const int pageSize = 100;
            int totalSynced = 0;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            while (true)
            {
                var response = await client.GetAsync($"/api/v1/productos?page={page}&pageSize={pageSize}", ct);
                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<ProductoPagedResultRaw>(content, _json);
                if (result?.Items == null || result.Items.Count == 0) break;

                foreach (var item in result.Items)
                {
                    var existing = await db.ProductosMirror.FindAsync([item.Id], ct);
                    if (existing is null)
                    {
                        db.ProductosMirror.Add(MapToEntity(item));
                    }
                    else
                    {
                        MapToExisting(existing, item);
                        db.ProductosMirror.Update(existing);
                    }
                    totalSynced++;
                }

                await db.SaveChangesAsync(ct);

                if (page * pageSize >= result.Total) break;
                page++;
            }

            _cbState.UpdateLastSync();
            _logger.LogInformation("[MirrorSync] Sync completado: {Count} productos actualizados", totalSynced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MirrorSync] Error durante sincronización de inventario");
        }
    }

    private static ProductoMirror MapToEntity(ProductoItemRaw item) => new()
    {
        Id = item.Id,
        Codigo = item.Codigo,
        Nombre = item.Nombre,
        Precio = item.Precio,
        Stock = item.Stock,
        EsActivo = item.EsActivo,
        Categoria = item.Categoria,
        UltimaSync = DateTime.UtcNow
    };

    private static void MapToExisting(ProductoMirror existing, ProductoItemRaw item)
    {
        existing.Codigo = item.Codigo;
        existing.Nombre = item.Nombre;
        existing.Precio = item.Precio;
        existing.Stock = item.Stock;
        existing.EsActivo = item.EsActivo;
        existing.Categoria = item.Categoria;
        existing.UltimaSync = DateTime.UtcNow;
    }

    // DTOs internos para deserializar la respuesta del Core
    private record ProductoPagedResultRaw(List<ProductoItemRaw>? Items, int Total, int Page, int PageSize);
    private record ProductoItemRaw(int Id, string Codigo, string Nombre, decimal Precio, int Stock, string Categoria, bool EsActivo);
}
