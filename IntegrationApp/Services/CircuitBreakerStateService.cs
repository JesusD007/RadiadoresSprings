namespace IntegrationApp.Services;

/// <summary>
/// Servicio singleton que mantiene el estado del Circuit Breaker hacia el Core API.
/// Todos los controllers consultan esta propiedad antes de decidir si proxean o sirven desde mirror.
/// </summary>
public interface ICircuitBreakerStateService
{
    bool CoreAvailable { get; }
    DateTimeOffset? UltimaSync { get; }
    void MarkCoreAvailable();
    void MarkCoreUnavailable();
    void UpdateLastSync();
}

public class CircuitBreakerStateService : ICircuitBreakerStateService
{
    private volatile bool _coreAvailable = false;
    private DateTimeOffset? _ultimaSync;
    private readonly ILogger<CircuitBreakerStateService> _logger;

    public CircuitBreakerStateService(ILogger<CircuitBreakerStateService> logger)
    {
        _logger = logger;
    }

    public bool CoreAvailable => _coreAvailable;
    public DateTimeOffset? UltimaSync => _ultimaSync;

    public void MarkCoreAvailable()
    {
        if (!_coreAvailable)
        {
            _coreAvailable = true;
            _logger.LogInformation("[CircuitBreaker] Core API DISPONIBLE — Circuit Breaker CERRADO");
        }
    }

    public void MarkCoreUnavailable()
    {
        if (_coreAvailable)
        {
            _coreAvailable = false;
            _logger.LogWarning("[CircuitBreaker] Core API NO DISPONIBLE — Circuit Breaker ABIERTO. Modo offline activado.");
        }
    }

    public void UpdateLastSync()
    {
        _ultimaSync = DateTimeOffset.UtcNow;
    }
}
