namespace IntegrationApp.Middleware;

/// <summary>
/// Genera o propaga el X-Correlation-Id para trazar cada request a través de todas las capas.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[HeaderName] = correlationId.ToString();
        context.Response.Headers[HeaderName] = correlationId.ToString();

        await _next(context);
    }
}
