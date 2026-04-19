using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationApp.Services;

/// <summary>
/// Cliente HTTP hacia el Core API (P2). Todos los requests al Core pasan por aquí.
/// Usa el HttpClient named "CoreApi" con el pipeline de resiliencia Polly v8.
/// </summary>
public interface ICoreApiClient
{
    Task<HttpResponseMessage> GetAsync(string path, string? bearerToken = null, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync(string path, object body, string? bearerToken = null, string? idempotencyKey = null, CancellationToken ct = default);
    Task<HttpResponseMessage> PutAsync(string path, object body, string? bearerToken = null, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(string path, string? bearerToken = null, CancellationToken ct = default);
}

public class CoreApiClient : ICoreApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoreApiClient> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CoreApiClient(IHttpClientFactory httpClientFactory, ILogger<CoreApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("CoreApi");

    private static void SetAuth(HttpRequestMessage req, string? bearerToken)
    {
        if (!string.IsNullOrWhiteSpace(bearerToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string path, string? bearerToken = null, CancellationToken ct = default)
    {
        var client = CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        SetAuth(req, bearerToken);
        return await client.SendAsync(req, ct);
    }

    public async Task<HttpResponseMessage> PostAsync(string path, object body, string? bearerToken = null, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var client = CreateClient();
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        SetAuth(req, bearerToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            req.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(req, ct);
    }

    public async Task<HttpResponseMessage> PutAsync(string path, object body, string? bearerToken = null, CancellationToken ct = default)
    {
        var client = CreateClient();
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var req = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        SetAuth(req, bearerToken);
        return await client.SendAsync(req, ct);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path, string? bearerToken = null, CancellationToken ct = default)
    {
        var client = CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Delete, path);
        SetAuth(req, bearerToken);
        return await client.SendAsync(req, ct);
    }
}
