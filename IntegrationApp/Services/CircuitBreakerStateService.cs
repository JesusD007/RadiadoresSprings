namespace IntegrationApp.Services;

/// <summary>
/// Servicio singleton que mantiene el estado del Circuit Breaker hacia el Core API.
/// — Todos los controllers lo consultan antes de decidir si proxean o sirven desde mirror.
/// — Expone el evento CoreRecuperado para que OperacionPendienteSyncService
///   inicie la sincronización inmediatamente cuando Core vuelve.
/// </summary>
public interface ICircuitBreakerStateService
{
    bool CoreAvailable { get; }
    DateTimeOffset? UltimaSync { get; }
    void MarkCoreAvailable();
    void MarkCoreUnavailable();
    void UpdateLastSync();

    /// <summary>
    /// Se dispara cuando el Core pasa de no-disponible a disponible.
    /// OperacionPendienteSyncService se suscribe para iniciar el replay inmediatamente.
    /// </summary>
    event EventHandler? CoreRecuperado;
}

public class CircuitBreakerStateService : ICircuitBreakerStateService
{
    private volatile bool _coreAvailable = false;
    private DateTimeOffset? _ultimaSync;
    private readonly ILogger<CircuitBreakerStateService> _logger;

    // Lock para el evento (evita condiciones de carrera al suscribir/disparar)
    private readonly object _eventLock = new();
    private EventHandler? _coreRecuperado;

    public CircuitBreakerStateService(ILogger<CircuitBreakerStateService> logger)
    {
        _logger = logger;
    }

    public bool CoreAvailable => _coreAvailable;
    public DateTimeOffset? UltimaSync => _ultimaSync;

    public event EventHandler? CoreRecuperado
    {
        add    { lock (_eventLock) { _coreRecuperado += value; } }
        remove { lock (_eventLock) { _coreRecuperado -= value; } }
    }

    public void MarkCoreAvailable()
    {
        if (!_coreAvailable)
        {
            _coreAvailable = true;
            _logger.LogInformation("[CircuitBreaker] Core API DISPONIBLE — Circuit Breaker CERRADO");

            // Disparar el evento en un thread pool para no bloquear CoreHealthCheckService
            EventHandler? handler;
            lock (_eventLock) { handler = _coreRecuperado; }
            if (handler is not null)
                Task.Run(() => handler.Invoke(this, EventArgs.Empty));
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
