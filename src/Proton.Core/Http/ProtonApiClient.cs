using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Proton.Core.Http;

/// <summary>
/// Thin HTTP wrapper around the Proton API, mirroring go-proton-api's resty-based Manager/Client
/// (manager_builder.go, client.go): it stamps every request with x-pm-appversion, attaches
/// x-pm-uid + a bearer token for authenticated calls, and - on a 401 - runs a caller-supplied
/// refresh callback once before retrying the original request exactly once.
/// </summary>
public sealed class ProtonApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AuthSession _session;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Func<CancellationToken, Task<bool>>? _unauthorizedHandler;

    public ProtonApiClient(ProtonApiOptions options, AuthSession session, HttpMessageHandler? handler = null)
    {
        _session = session;
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        AppVersion = options.AppVersion;

        // Proton's API uses exact PascalCase JSON keys (e.g. "ClientProof", "AccessToken") that
        // match this project's C# model property names directly - unlike JsonSerializerDefaults.Web,
        // no camelCase naming policy must be applied here.
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public string AppVersion { get; }

    /// <summary>
    /// Registered by AuthService after construction. Invoked at most once per failed request;
    /// should call the /auth/v4/refresh endpoint and update <see cref="AuthSession"/> in place,
    /// returning false if refresh itself failed (in which case the 401 is surfaced to the caller).
    /// </summary>
    public void SetUnauthorizedHandler(Func<CancellationToken, Task<bool>> handler)
    {
        _unauthorizedHandler = handler;
    }

    public Task<TResponse> GetAsync<TResponse>(string path, bool requiresAuth = true, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Get, path, body: null, requiresAuth, ct);

    public Task<TResponse> PostAsync<TResponse>(string path, object? body, bool requiresAuth = true, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Post, path, body, requiresAuth, ct);

    public Task<TResponse> PutAsync<TResponse>(string path, object? body, bool requiresAuth = true, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Put, path, body, requiresAuth, ct);

    public Task<TResponse> DeleteAsync<TResponse>(string path, bool requiresAuth = true, CancellationToken ct = default)
        => SendAsync<TResponse>(HttpMethod.Delete, path, body: null, requiresAuth, ct);

    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method, string path, object? body, bool requiresAuth, CancellationToken ct)
    {
        HttpResponseMessage response = await SendCoreAsync(method, path, body, requiresAuth, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized && requiresAuth && _unauthorizedHandler is not null)
        {
            response.Dispose();

            bool refreshed = await RefreshOnceAsync(ct).ConfigureAwait(false);
            if (refreshed)
            {
                response = await SendCoreAsync(method, path, body, requiresAuth, ct).ConfigureAwait(false);
            }
        }

        return await ReadResponseAsync<TResponse>(response, ct).ConfigureAwait(false);
    }

    private async Task<bool> RefreshOnceAsync(CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _unauthorizedHandler!(ct).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method, string path, object? body, bool requiresAuth, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("x-pm-appversion", AppVersion);

        if (requiresAuth)
        {
            if (!_session.IsAuthenticated)
            {
                throw new InvalidOperationException("This request requires an authenticated session.");
            }

            request.Headers.Add("x-pm-uid", _session.Uid);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: _jsonOptions);
        }

        return await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task<TResponse> ReadResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            string content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                ApiErrorResponse? error = TryDeserialize<ApiErrorResponse>(content);
                throw new ProtonApiException(
                    response.StatusCode,
                    error?.Code ?? 0,
                    error?.Error ?? $"Request failed with status {(int)response.StatusCode}.");
            }

            if (typeof(TResponse) == typeof(Unit))
            {
                return (TResponse)(object)Unit.Value;
            }

            TResponse? result = TryDeserialize<TResponse>(content);
            return result ?? throw new ProtonApiException(response.StatusCode, 0, "Unexpected empty response body.");
        }
    }

    private TResponse? TryDeserialize<TResponse>(string content)
    {
        return string.IsNullOrWhiteSpace(content)
            ? default
            : JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshLock.Dispose();
    }
}

/// <summary>Marker return type for API calls whose response body carries no useful data.</summary>
public sealed class Unit
{
    public static readonly Unit Value = new();
    private Unit()
    {
    }
}
