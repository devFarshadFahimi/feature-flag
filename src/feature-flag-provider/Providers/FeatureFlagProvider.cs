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

    private TokenResponse? _session;

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
        _username = settings.Username;
        _password = settings.Password;
    }

    /// <summary>Issues an ordinary user session and stores its access token only after a valid login response.</summary>
    /// <param name="cancellationToken">Cancels the HTTP request.</param>
    /// <returns>The issued user session, including its access token and expiration.</returns>
    public async Task<TokenResponse> IssueTokenAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, _authEndpoint))
        {
            Content = JsonContent.Create(new { email = _username, password = _password })
        };
        var token = await SendAsync<TokenResponse>(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(token.AccessToken)
            || token.AccessToken.Any(char.IsWhiteSpace)
            || token.AccessTokenExpiration <= _timeProvider.GetUtcNow())
        {
            throw new HttpRequestException("پاسخ ورود شامل توکن دسترسی معتبر نیست.");
        }

        Volatile.Write(ref _session, token);

        return token;
    }

    /// <summary>Uses a per-request bearer header; errors never become disabled flag decisions or trigger silent login.</summary>
    /// <param name="evaluation">The project, environment, context and feature names to evaluate.</param>
    /// <param name="cancellationToken">Cancels the HTTP request.</param>
    /// <returns>Authoritative evaluation decisions and snapshot freshness metadata.</returns>
    public async Task<EvaluationResponse> EvaluateAsync(EvaluationRequest evaluation, CancellationToken cancellationToken = default)
    {
        var token = Volatile.Read(ref _session);

        if (token is null || token.AccessTokenExpiration <= _timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("ابتدا توکن دسترسی دریافت کنید؛ توکن فعلی موجود نیست یا منقضی شده است.");
        }

        if (evaluation is null || evaluation.Context is null)
        {
            throw new ArgumentException("درخواست و زمینه ارزیابی الزامی است.", nameof(evaluation));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, EvaluationEndpoint))
        {
            Content = JsonContent.Create(evaluation)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token.AccessToken);

        var result = await SendAsync<EvaluationResponse>(request, cancellationToken);

        if (result.Features is null)
        {
            throw new HttpRequestException("پاسخ ارزیابی شامل فهرست پرچم‌ها نیست.");
        }

        return result;
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
