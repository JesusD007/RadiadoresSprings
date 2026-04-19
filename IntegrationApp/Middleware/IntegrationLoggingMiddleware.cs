using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace IntegrationApp.Middleware;

/// <summary>
/// Intercepta cada request/response y guarda IntegrationLogEntry en la BD.
/// NUNCA loguea: passwords, tokens completos, números de tarjeta.
/// </summary>
public class IntegrationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] _sensitiveFields = ["password", "token", "cardnumber", "cvv", "refreshtoken", "accesstoken"];
    private static readonly string[] _skipPaths = ["/health", "/swagger", "/hubs"];

    public IntegrationLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IntegrationDbContext db)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Omitir paths de sistema para no saturar los logs
        if (_skipPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString() ?? Guid.NewGuid().ToString("N");
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Capturar request body
        context.Request.EnableBuffering();
        string requestJson = string.Empty;
        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            requestJson = SanitizeJson(raw);
        }
        catch { /* no bloquear si falla la lectura */ }

        // Capturar response body
        var originalBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        sw.Stop();

        responseBuffer.Seek(0, SeekOrigin.Begin);
        string responseJson = string.Empty;
        try
        {
            var raw = await new StreamReader(responseBuffer).ReadToEndAsync();
            responseBuffer.Seek(0, SeekOrigin.Begin);
            responseJson = SanitizeJson(raw);
        }
        catch { /* no bloquear */ }

        await responseBuffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        // Guardar en BD
        try
        {
            db.IntegrationLogs.Add(new IntegrationLogEntry
            {
                Endpoint = $"{context.Request.Method} {context.Request.Path}",
                Direccion = "IN",
                RequestJson = requestJson.Length > 4000 ? requestJson[..4000] : requestJson,
                ResponseJson = responseJson.Length > 4000 ? responseJson[..4000] : responseJson,
                HttpStatus = context.Response.StatusCode,
                LatenciaMs = (int)sw.ElapsedMilliseconds,
                DesdeCache = context.Items.ContainsKey("FromMirror"),
                CorrelationId = correlationId,
                UserId = userId,
                Layer = "Integracion",
                Fecha = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch { /* logging no debe romper el flujo principal */ }
    }

    private static string SanitizeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        foreach (var field in _sensitiveFields)
        {
            json = Regex.Replace(json, $@"(""{field}""\s*:\s*)""\s*[^""]*""",
                $"$1\"[REDACTED]\"", RegexOptions.IgnoreCase);
        }
        return json;
    }
}
