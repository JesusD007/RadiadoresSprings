namespace Core.API.Services;

/// <summary>
/// Implementación de IClockService que convierte UTC al huso horario configurado en
/// appsettings.json → "TimeZoneId". Esto garantiza que las fechas de negocio
/// (órdenes, ventas, sesiones de caja) reflejen la hora local del negocio
/// independientemente del timezone del servidor o del host de despliegue.
///
/// Identificadores válidos (Windows): "Central Standard Time", "Mountain Standard Time", etc.
/// Identificadores válidos (Linux/macOS): "America/Mexico_City", "America/Monterrey", etc.
/// </summary>
public sealed class ClockService : IClockService
{
    private readonly TimeZoneInfo _tz;

    public ClockService(IConfiguration config)
    {
        var tzId = config["TimeZoneId"] ?? "Central Standard Time";

        // TimeZoneInfo.FindSystemTimeZoneById lanza si el ID no existe.
        // Intentamos el ID configurado; si falla (por ejemplo en Linux con ID de Windows),
        // caemos al UTC para no romper el arranque.
        try
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (TimeZoneNotFoundException)
        {
            _tz = TimeZoneInfo.Utc;
        }
    }

    /// <inheritdoc/>
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
}
