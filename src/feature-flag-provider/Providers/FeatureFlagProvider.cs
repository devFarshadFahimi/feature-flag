using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FeatureFlag.Provider.Contracts;
using FeatureFlag.Provider.Options;
using FeatureFlag.Provider.ResponseModels;
using Microsoft.Extensions.Options;

namespace FeatureFlag.Provider.Providers;

public sealed class FeatureFlagProvider
{
    internal const string InvalidConfigurationMessage = "تنظیمات سرویس پرچم ویژگی معتبر نیست؛ نشانی پایه، مسیر ورود، نام کاربری و گذرواژه را بررسی کنید.";

    private const string EvaluationEndpoint = "Evaluations";

    private const string BearerScheme = "Bearer";

    private readonly HttpClient _client;
    private readonly TimeProvider _timeProvider;
    private readonly Uri _baseUri;

    private readonly string _authEndpoint;

    private readonly string _username;

    private readonly string _password;

    private readonly string _refreshEndpoint;
    private readonly SemaphoreSlim _tokenGate = new(1, 1);

    private TokenResponse? _tokens;

    public FeatureFlagProvider(HttpClient client, TimeProvider timeProvider, IOptions<FeatureFlagProviderOptions> options)
    {
        var settings = options.Value;

        if (!settings.IsValid())
        {
            throw new ArgumentException(InvalidConfigurationMessage, nameof(options));
        }

        _client = client;
        _timeProvider = timeProvider;
        _baseUri = new Uri(settings.BaseUri.TrimEnd('/') + "/");
        _authEndpoint = settings.AuthEndpoint;
        _refreshEndpoint = settings.RefreshEndpoint;
        _username = settings.Username;
        _password = settings.Password;
    }

    /// <summary>Logs in explicitly and replaces stored tokens; a failed login clears previous credentials.</summary>
    /// <param name="cancellationToken">Cancels the HTTP request.</param>
    /// <returns>The initial tokens and authenticated user. Later refreshes are handled internally.</returns>
    public async Task<TokenResponse> IssueTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenGate.WaitAsync(cancellationToken);

        try
        {
            Volatile.Write(ref _tokens, null);

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

    /// <summary>Refreshes expired tokens or retries one unauthorized evaluation with rotated tokens; other errors propagate.</summary>
    /// <param name="evaluation">The project, environment, context and feature names to evaluate.</param>
    /// <param name="cancellationToken">Cancels requests and waiting for another token operation.</param>
    /// <returns>Authoritative evaluation decisions and snapshot freshness metadata.</returns>
    public async Task<EvaluationResponse> EvaluateAsync(EvaluationRequest evaluation, CancellationToken cancellationToken = default)
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
            catch (HttpRequestException retryException) when (retryException.StatusCode == HttpStatusCode.Unauthorized)
            {
                Interlocked.CompareExchange(ref _tokens, null, tokens);

                throw;
            }
        }

        if (result.Features is null)
        {
            throw new HttpRequestException("پاسخ ارزیابی شامل فهرست پرچم‌ها نیست.");
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
    /// <param name="rejectedTokens">The token pair used by the rejected request; a newer pair is reused instead of refreshed twice.</param>
    /// <param name="cancellationToken">Cancels the operation and its database or network requests.</param>
    /// <returns>The current token pair after a serialized refresh, or the newer pair already obtained by another request.</returns>
    private async Task<TokenResponse> RefreshTokensAsync(TokenResponse rejectedTokens, CancellationToken cancellationToken)
    {
        await _tokenGate.WaitAsync(cancellationToken);

        try
        {
            var current = ReadTokens();

            if (!ReferenceEquals(current, rejectedTokens)
                && current.AccessTokenExpiration > _timeProvider.GetUtcNow())
            {
                return current;
            }

            // Clear before sending: a lost or invalid response may still have consumed the refresh token.
            Volatile.Write(ref _tokens, null);

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
        return Volatile.Read(ref _tokens)
            ?? throw new InvalidOperationException("توکن دسترسی موجود نیست؛ ابتدا وارد شوید.");
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

        if (string.IsNullOrWhiteSpace(tokens.AccessToken)
            || tokens.AccessToken.Any(char.IsWhiteSpace)
            || tokens.AccessTokenExpiration <= now
            || string.IsNullOrWhiteSpace(tokens.RefreshToken)
            || tokens.RefreshToken.Any(char.IsWhiteSpace)
            || tokens.RefreshTokenExpiration <= now)
        {
            throw new HttpRequestException("پاسخ احراز هویت شامل توکن‌های معتبر نیست.");
        }
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.SendAsync(request, cancellationToken);

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

            var result = await response.Content.ReadFromJsonAsync<T>(JSONSerializeExtensions.ResponseJSON, cancellationToken);

            return result ?? throw new HttpRequestException("پاسخ سرویس پرچم ویژگی خالی است.");
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("قالب پاسخ سرویس پرچم ویژگی معتبر نیست.", exception);
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
}
