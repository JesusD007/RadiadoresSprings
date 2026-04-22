using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationApp.Services;

/// <summary>
/// Proporciona un JWT válido para autenticar las llamadas M2M de IntegrationApp
/// hacia Core.API (usuario "servicio_web").
/// — Cachea el token hasta 2 minutos antes de su expiración.
/// — Thread-safe: usa SemaphoreSlim para evitar refreshes concurrentes.
/// — Las credenciales se leen de variables de entorno en Render:
///     SERVICIO_WEB_USERNAME  (default: servicio_web)
///     SERVICIO_WEB_PASSWORD  (obligatoria)
/// </summary>
public interface ICoreTokenService
{
    /// <summary>Devuelve un token JWT vigente, renovándolo si es necesario.</summary>
    Task<string> GetTokenAsync(CancellationToken ct = default);
}

public class CoreTokenService : ICoreTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CoreTokenService> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    // Renovar 2 minutos antes de que expire para evitar ventanas de 401
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    public CoreTokenService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<CoreTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Lectura optimista sin lock
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry - RefreshMargin)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check después de adquirir el lock
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry - RefreshMargin)
                return _cachedToken;

            _cachedToken = await FetchTokenAsync(ct);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> FetchTokenAsync(CancellationToken ct)
    {
        var username = Environment.GetEnvironmentVariable("SERVICIO_WEB_USERNAME")
                    ?? _config["ServiceAccount:Username"]
                    ?? "servicio_web";

        var password = Environment.GetEnvironmentVariable("SERVICIO_WEB_PASSWORD")
                    ?? _config["ServiceAccount:Password"];

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "La variable de entorno SERVICIO_WEB_PASSWORD no está configurada. " +
                "Establécela en Render → Environment → SERVICIO_WEB_PASSWORD.");

        var client = _httpClientFactory.CreateClient("CoreApi");

        var body = JsonSerializer.Serialize(new { username, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        _logger.LogInformation("[CoreToken] Obteniendo JWT del Core para usuario '{User}'", username);

        using var response = await client.PostAsync("/api/v1/auth/login", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Login de servicio en Core falló ({(int)response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        // El Core devuelve { token: "...", expiresAt: "..." } o { token: "...", expiresIn: 480 }
        var token = doc.RootElement.TryGetProperty("token", out var tokenProp)
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "La respuesta de login del Core no contiene el campo 'token'.");

        // Calcular expiración
        if (doc.RootElement.TryGetProperty("expiresAt", out var expiresAtProp) &&
            DateTimeOffset.TryParse(expiresAtProp.GetString(), out var expiresAt))
        {
            _tokenExpiry = expiresAt;
        }
        else if (doc.RootElement.TryGetProperty("expiresIn", out var expiresInProp) &&
                 expiresInProp.TryGetInt32(out var expiresInMinutes))
        {
            _tokenExpiry = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes);
        }
        else
        {
            // Fallback: asumir 8 horas (configuración por defecto del Core)
            _tokenExpiry = DateTimeOffset.UtcNow.AddHours(8);
        }

        _logger.LogInformation("[CoreToken] JWT obtenido. Válido hasta {Expiry:HH:mm:ss} UTC", _tokenExpiry);
        return token;
    }
}
