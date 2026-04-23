using System.Text.Json;

namespace IntegrationApp.Helpers;

/// <summary>
/// Utilidad para extraer el cuerpo ("data") de respuestas que vienen 
/// envueltas en el contenedor estándar ApiResponse del Core.API.
/// </summary>
public static class ProxyHelper
{
    public static T? Unwrap<T>(string content, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(content)) return default;

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("data", out var dataElement))
        {
            return JsonSerializer.Deserialize<T>(dataElement.GetRawText(), options);
        }
        
        // Fallback por si la respuesta no estaba envuelta
        return JsonSerializer.Deserialize<T>(content, options);
    }
}
