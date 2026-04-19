using IntegrationApp.Services;

namespace IntegrationApp.BackgroundServices;

/// <summary>
/// Hace ping al /health del Core cada N segundos (configurable).
/// Actualiza el estado del Circuit Breaker en CircuitBreakerStateService.
/// - 3 fallos consecutivos → CoreAvailable = false (CB abierto)
/// - 1 respuesta exitosa → CoreAvailable = true (CB cerrado)
/// </summary>
public class CoreHealthCheckService : BackgroundService
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CoreHealthCheckService> _logger;

    private int _consecutiveFailures = 0;

    public CoreHealthCheckService(
        ICircuitBreakerStateService cbState,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<CoreHealthCheckService> logger)
    {
        _cbState = cbState;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.GetValue<int>("CoreApi:HealthCheckIntervalSeconds", 15);
        var failureThreshold = _config.GetValue<int>("CoreApi:CircuitBreakerFailureThreshold", 3);

        _logger.LogInformation("[HealthCheck] Iniciando ping al Core cada {Interval}s", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            await CheckCoreHealthAsync(failureThreshold, stoppingToken);
        }
    }

    private async Task CheckCoreHealthAsync(int failureThreshold, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CoreApi");
            using var response = await client.GetAsync("/health", ct);

            if (response.IsSuccessStatusCode)
            {
                _consecutiveFailures = 0;
                _cbState.MarkCoreAvailable();
                _logger.LogDebug("[HealthCheck] Core disponible — ping exitoso");
            }
            else
            {
                HandleFailure(failureThreshold, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            HandleFailure(failureThreshold, ex.Message);
        }
    }

    private void HandleFailure(int threshold, string reason)
    {
        _consecutiveFailures++;
        _logger.LogWarning("[HealthCheck] Fallo #{Count} al contactar Core: {Reason}", _consecutiveFailures, reason);

        if (_consecutiveFailures >= threshold)
        {
            _cbState.MarkCoreUnavailable();
        }
    }
}
