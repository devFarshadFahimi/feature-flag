using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FeatureFlag.Provider.Contracts;
using FeatureFlag.Provider.Options;
using FeatureFlag.Provider.ResponseModels;
using Microsoft.Extensions.Options;

namespace FeatureFlag.Provider.Providers;

public sealed class FeatureFlagProvider
{
    internal const string InvalidConfigurationMessage = "تنظیمات سرویس پرچم ویژگی معتبر نیست؛ نشانی پایه، مسیر ورود، نام کاربری و گذرواژه را بررسی کنید.";

    internal const string HttpClientName = "FeatureFlag.Provider";

    private const string EvaluationEndpoint = "Evaluations";

    private const string BearerScheme = "Bearer";

    private const int MaxEntryBytes = 256 * 1024;

    private const int MaxRequestBytes = 64 * 1024;

    private readonly HttpClient? _client;

    private readonly IHttpClientFactory? _clientFactory;

    private readonly FeatureFlagProviderOptions _settings;

    private readonly object _cacheGate = new();

    private readonly Dictionary<string, CachedEvaluation> _evaluations = new(StringComparer.Ordinal);

    private readonly TimeProvider _timeProvider;

    private readonly Uri _baseUri;

    private readonly string _authEndpoint;

    private readonly string _username;

    private readonly string _password;

    private readonly string _refreshEndpoint;

    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    private int _cacheBytes;

    private long _identityGeneration;

    private TokenResponse? _tokens;

    public FeatureFlagProvider(HttpClient client, TimeProvider timeProvider, IOptions<FeatureFlagProviderOptions> options)
        : this(client, null, timeProvider, options)
    {
    }

    internal FeatureFlagProvider(IHttpClientFactory clientFactory, TimeProvider timeProvider, IOptions<FeatureFlagProviderOptions> options)
        : this(null, clientFactory, timeProvider, options)
    {
    }

    private FeatureFlagProvider(HttpClient? client, IHttpClientFactory? clientFactory, TimeProvider timeProvider, IOptions<FeatureFlagProviderOptions> options)
    {
        var settings = options.Value;

        if (!settings.IsValid())
        {
            throw new ArgumentException(InvalidConfigurationMessage, nameof(options));
        }

        _client = client;
        _clientFactory = clientFactory;
        // Capture validated settings so later options mutation cannot change the security boundary.
        _settings = new FeatureFlagProviderOptions
        {
            RequestTimeoutSeconds = settings.RequestTimeoutSeconds,
            EnableLastKnownGoodFallback = settings.EnableLastKnownGoodFallback,
            MaxStaleSeconds = settings.MaxStaleSeconds,
            MaxCachedEvaluations = settings.MaxCachedEvaluations,
            MaxCacheBytes = settings.MaxCacheBytes
        };
        _timeProvider = timeProvider;
        _baseUri = new Uri(settings.BaseUri.TrimEnd('/') + "/");
        _authEndpoint = settings.AuthEndpoint;
        _refreshEndpoint = settings.RefreshEndpoint;
        _username = settings.Username;
        _password = settings.Password;
    }

    /// <summary>Logs in explicitly and replaces stored tokens; a failed login clears previous credentials.</summary>
    /// <param name = "cancellationToken">Cancels the HTTP request.</param>
    /// <returns>The initial tokens and authenticated user. Later refreshes are handled internally.</returns>
    public async Task<TokenResponse> IssueTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            ClearIdentity();

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, _authEndpoint))
            {
                Content = JsonContent.Create(new { email = _username, password = _password })
            };
            var tokens = await SendAsync<TokenResponse>(request, cancellationToken);

            ValidateTokens(tokens);
            Volatile.Write(ref _tokens, tokens);

            return tokens;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    /// <summary>Evaluates remotely, refreshing once when required; optional outage replay requires an identical successful request.</summary>
    /// <param name = "evaluation">The project, environment, context and feature names to evaluate.</param>
    /// <param name = "cancellationToken">Cancels requests and waiting for another token operation.</param>
    /// <returns>Evaluation decisions with original server timestamps and an explicit provider replay indicator.</returns>
    public async Task<EvaluationResponse> EvaluateAsync(EvaluationRequest evaluation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (evaluation is null || evaluation.Context is null)
        {
            throw new ArgumentException("درخواست و زمینه ارزیابی الزامی است.", nameof(evaluation));
        }

        // Freeze mutable collections once so the cache identity describes exactly what was sent.
        var requestBytes = JsonSerializer.SerializeToUtf8Bytes(evaluation, JSONSerializeExtensions.ResponseJSON);
        var frozen = JsonSerializer.Deserialize<EvaluationRequest>(requestBytes, JSONSerializeExtensions.ResponseJSON)!;
        var cacheable = _settings.EnableLastKnownGoodFallback && requestBytes.Length <= MaxRequestBytes && (frozen.FeatureNames is null || frozen.FeatureNames.Count <= 100);
        var key = cacheable ? Convert.ToHexString(SHA256.HashData(requestBytes)) : null;
        var generation = Volatile.Read(ref _identityGeneration);

        try
        {
            var result = await EvaluateCoreAsync(frozen, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (key is not null)
            {
                RememberEvaluation(key, result, generation);
            }

            return result with
            {
                FromCache = false
            };
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested && IsTransient(exception) && key is not null)
        {
            lock (_cacheGate)
            {
                if (generation == _identityGeneration && Volatile.Read(ref _tokens) is not null && _evaluations.TryGetValue(key, out var cached) && cached.ExpiresAt > _timeProvider.GetUtcNow())
                {
                    var result = JsonSerializer.Deserialize<EvaluationResponse>(cached.Response, JSONSerializeExtensions.ResponseJSON)!;

                    cancellationToken.ThrowIfCancellationRequested();

                    return result with
                    {
                        FromCache = true,
                        Stale = true
                    };
                }
            }

            throw;
        }
    }

    private static bool IsTransient(HttpRequestException exception)
    {
        return exception.StatusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int?)exception.StatusCode >= 500;
    }

    private async Task<EvaluationResponse> EvaluateCoreAsync(EvaluationRequest evaluation, CancellationToken cancellationToken)
    {
        if (evaluation is null || evaluation.Context is null)
        {
            throw new ArgumentException("درخواست و زمینه ارزیابی الزامی است.", nameof(evaluation));
        }

        var tokens = await GetTokensAsync(cancellationToken);

        if (tokens.AccessTokenExpiration <= _timeProvider.GetUtcNow())
        {
            tokens = await RefreshTokensAsync(tokens, cancellationToken);
        }

        EvaluationResponse result;

        try
        {
            result = await SendEvaluationAsync(evaluation, tokens, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            tokens = await RefreshTokensAsync(tokens, cancellationToken);
            try
            {
                result = await SendEvaluationAsync(evaluation, tokens, cancellationToken);
            }
            catch (HttpRequestException retryException) when (retryException.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                ClearIdentity(tokens);

                throw;
            }
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
        {
            ClearIdentity(tokens);

            throw;
        }

        if (result.Features is null)
        {
            throw new HttpRequestException("پاسخ ارزیابی شامل فهرست پرچم‌ها نیست.", null, HttpStatusCode.OK);
        }

        return result;
    }

    private async Task<EvaluationResponse> SendEvaluationAsync(EvaluationRequest evaluation, TokenResponse tokens, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, EvaluationEndpoint))
        {
            Content = JsonContent.Create(evaluation)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, tokens.AccessToken);

        return await SendAsync<EvaluationResponse>(request, cancellationToken);
    }

    /// <summary>Serializes one-time refresh rotation and reuses tokens already refreshed by another caller.</summary>
    /// <param name = "rejectedTokens">The token pair used by the rejected request; a newer pair is reused instead of refreshed twice.</param>
    /// <param name = "cancellationToken">Cancels the operation and its database or network requests.</param>
    /// <returns>The current token pair after a serialized refresh, or the newer pair already obtained by another request.</returns>
    private async Task<TokenResponse> RefreshTokensAsync(TokenResponse rejectedTokens, CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            var current = ReadTokens();

            if (!ReferenceEquals(current, rejectedTokens) && current.AccessTokenExpiration > _timeProvider.GetUtcNow())
            {
                return current;
            }

            // Clear before sending: a lost or invalid response may still have consumed the refresh token.
            ClearIdentity();

            if (current.RefreshTokenExpiration <= _timeProvider.GetUtcNow())
            {
                throw new InvalidOperationException("توکن تمدید منقضی شده است؛ دوباره وارد شوید.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, _refreshEndpoint))
            {
                Content = JsonContent.Create(new { accessToken = current.AccessToken, refreshToken = current.RefreshToken })
            };
            var tokens = await SendAsync<TokenResponse>(request, cancellationToken);

            ValidateTokens(tokens);
            Volatile.Write(ref _tokens, tokens);

            return tokens;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private TokenResponse ReadTokens()
    {
        return Volatile.Read(ref _tokens) ?? throw new InvalidOperationException("توکن دسترسی موجود نیست؛ ابتدا وارد شوید.");
    }

    private async Task<TokenResponse> GetTokensAsync(CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            return ReadTokens();
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private void ValidateTokens(TokenResponse tokens)
    {
        var now = _timeProvider.GetUtcNow();

        if (string.IsNullOrWhiteSpace(tokens.AccessToken) || tokens.AccessToken.Any(char.IsWhiteSpace) || tokens.AccessTokenExpiration <= now || string.IsNullOrWhiteSpace(tokens.RefreshToken) || tokens.RefreshToken.Any(char.IsWhiteSpace) || tokens.RefreshTokenExpiration <= now)
        {
            throw new HttpRequestException("پاسخ احراز هویت شامل توکن‌های معتبر نیست.", null, HttpStatusCode.OK);
        }
    }

    /// <summary>Invalidates identity-bound decisions; a rejected request can clear only the tokens it actually used.</summary>
    private void ClearIdentity(TokenResponse? expectedTokens = null)
    {
        lock (_cacheGate)
        {
            if (expectedTokens is not null && !ReferenceEquals(Volatile.Read(ref _tokens), expectedTokens))
            {
                return;
            }

            Volatile.Write(ref _tokens, null);
            Interlocked.Increment(ref _identityGeneration);
            _evaluations.Clear();
            _cacheBytes = 0;
        }
    }

    /// <summary>Bounds replay by both local retention and the server snapshot's remaining freshness budget.</summary>
    private void RememberEvaluation(string key, EvaluationResponse result, long generation)
    {
        var now = _timeProvider.GetUtcNow();

        if (result.Features.Count > 100 || result.MaxStaleSeconds is < 0 or > 3600 || result.SnapshotLoadedAt > now || result.EvaluatedAt > now)
        {
            return;
        }

        var expiresAt = new[]
        {
            now.AddSeconds(_settings.MaxStaleSeconds),
            result.EvaluatedAt.AddSeconds(_settings.MaxStaleSeconds),
            result.SnapshotLoadedAt.AddSeconds(result.MaxStaleSeconds)
        }.Min();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, JSONSerializeExtensions.ResponseJSON);

        if (expiresAt <= now || bytes.Length > MaxEntryBytes || bytes.Length > _settings.MaxCacheBytes)
        {
            return;
        }

        lock (_cacheGate)
        {
            if (generation != _identityGeneration || Volatile.Read(ref _tokens) is null)
            {
                return;
            }

            var expiredEntries = _evaluations
                .Where(entry => entry.Value.ExpiresAt <= now || entry.Key == key)
                .ToArray();

            foreach (var expired in expiredEntries)
            {
                _cacheBytes -= expired.Value.Response.Length;
                _evaluations.Remove(expired.Key);
            }

            while (_evaluations.Count >= _settings.MaxCachedEvaluations || _cacheBytes + bytes.Length > _settings.MaxCacheBytes)
            {
                var oldest = _evaluations.MinBy(entry => entry.Value.ExpiresAt);

                _cacheBytes -= oldest.Value.Response.Length;
                _evaluations.Remove(oldest.Key);
            }

            _evaluations.Add(key, new CachedEvaluation(bytes, expiresAt));
            _cacheBytes += bytes.Length;
        }
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeout.CancelAfter(TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds));

            using var operationClient = _clientFactory?.CreateClient(HttpClientName);
            var client = operationClient ?? _client!;
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "احراز هویت ناموفق بود؛ اطلاعات ورود یا اعتبار توکن را بررسی کنید.",
                    HttpStatusCode.Forbidden => "اجازه دسترسی به پروژه یا ارزیابی پرچم را ندارید.",
                    _ => $"درخواست سرویس پرچم ویژگی با کد {(int)response.StatusCode} ناموفق بود."
                };

                throw new HttpRequestException(message, null, response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<T>(JSONSerializeExtensions.ResponseJSON, timeout.Token);

            return result ?? throw new HttpRequestException("پاسخ سرویس پرچم ویژگی خالی است.", null, response.StatusCode);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("قالب پاسخ سرویس پرچم ویژگی معتبر نیست.", exception, HttpStatusCode.OK);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is null)
        {
            throw new HttpRequestException("دریافت پاسخ معتبر از سرویس پرچم ویژگی ناموفق بود.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HttpRequestException("زمان انتظار برای پاسخ سرویس پرچم ویژگی پایان یافت.", exception);
        }
    }

    private sealed record CachedEvaluation(byte[] Response, DateTimeOffset ExpiresAt);
}
