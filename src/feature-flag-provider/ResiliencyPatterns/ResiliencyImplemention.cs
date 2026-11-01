using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Registry;
using Polly.Retry;
using Polly.Timeout;

namespace FeatureFlag.Provider.ResiliencyPatterns;

public class ResiliencyOptions
{
    // Retry Configuration
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public DelayBackoffType BackOffType { get; set; } = DelayBackoffType.Exponential;
    public bool UseJitter { get; set; } = true;

    // Circuit Breaker Configuration
    public double FailureRatio { get; set; } = 0.5;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
    public int MinimumThroughput { get; set; } = 10;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    // Timeout Configuration
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    // Concurrency Limiter Configuration
    public int PermitLimit { get; set; } = 100;
    public int QueueLimit { get; set; } = 0;

    // Fallback Configuration
    public bool EnableFallback { get; set; } = false;
    public Func<Exception, object?>? FallbackValueFactory
    {
        get; set;
    }

    // Enable/Disable Strategies
    public bool EnableRetry { get; set; } = true;
    public bool EnableCircuitBreaker { get; set; } = true;
    public bool EnableTimeout { get; set; } = true;
    public bool EnableConcurrencyLimiter { get; set; } = false;
}


public class ResiliencyService
{
    private readonly ILogger<ResiliencyService> _logger;
    private readonly ResiliencyOptions _options;
    private readonly ResiliencePipelineRegistry<string> _pipelineRegistry;

    public ResiliencyService(
        ILogger<ResiliencyService> logger,
        IOptions<ResiliencyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _pipelineRegistry = new ResiliencePipelineRegistry<string>();
    }

    /// <summary>
    /// Gets or creates a resilience pipeline for the given key.
    /// </summary>
    /// <param name="pipelineKey">The key identifying the cached resilience pipeline.</param>
    /// <returns>The cached pipeline configured with the enabled resilience strategies.</returns>
    public ResiliencePipeline GetPipeline(string pipelineKey = "default")
    {
        return _pipelineRegistry.GetOrAddPipeline(pipelineKey, ConfigurePipeline);
    }

    /// <summary>
    /// Gets or creates a generic resilience pipeline for the given key.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the resilience pipeline.</typeparam>
    /// <param name="pipelineKey">The key identifying the cached resilience pipeline.</param>
    /// <returns>The cached pipeline configured with the enabled resilience strategies.</returns>
    public ResiliencePipeline<TResult> GetPipeline<TResult>(string pipelineKey = "default")
    {
        return _pipelineRegistry.GetOrAddPipeline<TResult>(pipelineKey, ConfigurePipeline);
    }

    private void ConfigurePipeline(ResiliencePipelineBuilder builder)
    {
        // Order matters! Timeout should be innermost, Circuit Breaker outermost
        if (_options.EnableTimeout)
        {
            _ = builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = _options.Timeout,
                OnTimeout = args =>
                {
                    _logger.LogWarning("Timeout occurred after {TimeoutSeconds}s", args.Timeout.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableRetry)
        {
            _ = builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = _options.RetryDelay,
                BackoffType = _options.BackOffType,
                UseJitter = _options.UseJitter,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<Exception>(ex => ex.Message.Contains("transient")),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber} after {Delay}ms due to: {ExceptionMessage}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableCircuitBreaker)
        {
            _ = builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = _options.FailureRatio,
                SamplingDuration = _options.SamplingDuration,
                MinimumThroughput = _options.MinimumThroughput,
                BreakDuration = _options.BreakDuration,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker opened. Failure ratio: {FailureRatio}",
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed");
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableConcurrencyLimiter)
        {
            _ = builder.AddConcurrencyLimiter(_options.PermitLimit, _options.QueueLimit);
        }
    }

    private void ConfigurePipeline<TResult>(ResiliencePipelineBuilder<TResult> builder)
    {
        // Order matters! Timeout should be innermost, Circuit Breaker outermost
        if (_options.EnableTimeout)
        {
            _ = builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = _options.Timeout,
                OnTimeout = args =>
                {
                    _logger.LogWarning("Timeout occurred after {TimeoutSeconds}s", args.Timeout.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableFallback)
        {
            _ = builder.AddFallback(new FallbackStrategyOptions<TResult>
            {
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<Exception>(),
                FallbackAction = args =>
                {
                    var fallbackValue = _options.FallbackValueFactory?.Invoke(args.Outcome.Exception!)
                        ?? default(TResult);

                    _logger.LogWarning(
                        "Fallback triggered due to: {ExceptionMessage}. Using fallback value.",
                        args.Outcome.Exception?.Message);

                    return Outcome.FromResultAsValueTask((TResult)fallbackValue!);
                }
            });
        }

        if (_options.EnableRetry)
        {
            _ = builder.AddRetry(new RetryStrategyOptions<TResult>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = _options.RetryDelay,
                BackoffType = _options.BackOffType,
                UseJitter = _options.UseJitter,
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(result => result is null),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber} after {Delay}ms due to: {ExceptionMessage}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableCircuitBreaker)
        {
            _ = builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
            {
                FailureRatio = _options.FailureRatio,
                SamplingDuration = _options.SamplingDuration,
                MinimumThroughput = _options.MinimumThroughput,
                BreakDuration = _options.BreakDuration,
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker opened. Failure ratio: {FailureRatio}",
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed");
                    return ValueTask.CompletedTask;
                }
            });
        }

        if (_options.EnableConcurrencyLimiter)
        {
            _ = builder.AddConcurrencyLimiter(_options.PermitLimit, _options.QueueLimit);
        }
    }
}

public static class ResiliencyServiceExtensions
{
    /// <summary>
    /// Adds the ResiliencyService with default configuration.
    /// </summary>
    /// <param name="services">The consuming application's service collection.</param>
    /// <returns>The service collection for further registrations.</returns>
    public static IServiceCollection AddResiliencyService(this IServiceCollection services)
    {
        _ = services.Configure<ResiliencyOptions>(_ => { }); // Use defaults
        _ = services.AddSingleton<ResiliencyService>();
        return services;
    }

    /// <summary>
    /// Adds the ResiliencyService with custom configuration.
    /// </summary>
    /// <param name="services">The consuming application's service collection.</param>
    /// <param name="configure">The callback configuring resilience strategy options.</param>
    /// <returns>The service collection for further registrations.</returns>
    public static IServiceCollection AddResiliencyService(
        this IServiceCollection services,
        Action<ResiliencyOptions> configure)
    {
        _ = services.Configure(configure);
        _ = services.AddSingleton<ResiliencyService>();
        return services;
    }

    /// <summary>
    /// Adds the ResiliencyService with configuration from appsettings.json.
    /// </summary>
    /// <param name="services">The consuming application's service collection.</param>
    /// <param name="configuration">The configuration containing the resilience settings section.</param>
    /// <param name="sectionName">The configuration section from which to bind resilience options.</param>
    /// <returns>The service collection for further registrations.</returns>
    public static IServiceCollection AddResiliencyService(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Resiliency")
    {
        _ = services.Configure<ResiliencyOptions>(configuration.GetSection(sectionName));
        _ = services.AddSingleton<ResiliencyService>();
        return services;
    }

    /// <summary>
    /// Adds HttpClient with resilience pipeline.
    /// </summary>
    /// <param name="services">The consuming application's service collection.</param>
    /// <param name="name">The name identifying the HTTP client registration.</param>
    /// <param name="configureClient">The optional callback configuring the named HTTP client.</param>
    /// <param name="configureResiliency">The optional callback configuring the client's resilience strategies.</param>
    /// <returns>The HTTP client builder for further transport configuration.</returns>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null,
        Action<ResiliencyOptions>? configureResiliency = null)
    {
        var options = new ResiliencyOptions();
        configureResiliency?.Invoke(options);

        var builder = services.AddHttpClient(name, configureClient ?? (_ => { }));

        // Add resilience handler
        builder.AddHttpMessageHandler(() => new ResilienceHttpMessageHandler(
            new ResiliencyService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ResiliencyService>.Instance,
                Microsoft.Extensions.Options.Options.Create(options))));

        return builder;
    }
}

public class ResilienceHttpMessageHandler : DelegatingHandler
{
    private readonly ResiliencyService _resiliencyService;

    public ResilienceHttpMessageHandler(ResiliencyService resiliencyService)
    {
        _resiliencyService = resiliencyService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var pipeline = _resiliencyService.GetPipeline<HttpResponseMessage>();

        return await pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct),
            cancellationToken);
    }
}


// 5. Usage Examples
public class ExternalApiService(ResiliencyService resiliencyService, HttpClient httpClient)
{
    public async Task<string> GetDataAsync()
    {
        var pipeline = resiliencyService.GetPipeline<string>();

        return await pipeline.ExecuteAsync(
            async ct =>
        {
            var response = await httpClient.GetAsync("/api/data", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }, CancellationToken.None);
    }
}

//// Option 1: Use defaults
//builder.Services.AddResiliencyService();

//// Option 2: Custom configuration
//builder.Services.AddResiliencyService(options =>
//{
//    options.MaxRetryAttempts = 5;
//    options.RetryDelay = TimeSpan.FromSeconds(2);
//    options.Timeout = TimeSpan.FromSeconds(10);
//    options.EnableCircuitBreaker = true;
//    options.FailureRatio = 0.3;
//});

//// Option 3: From appsettings.json
//builder.Services.AddResiliencyService(builder.Configuration);

//// Option 4: Resilient HttpClient
//builder.Services.AddResilientHttpClient("ExternalApi",
//    client => client.BaseAddress = new Uri("https://api.example.com"),
//    options =>
//    {
//        options.MaxRetryAttempts = 3;
//        options.Timeout = TimeSpan.FromSeconds(5);
//    });


////appsettings.json Configuration

//{
//  "Resiliency": {
//    "MaxRetryAttempts": 3,
//    "RetryDelay": "00:00:01",
//    "BackoffType": "Exponential",
//    "UseJitter": true,
//    "FailureRatio": 0.5,
//    "SamplingDuration": "00:00:10",
//    "MinimumThroughput": 10,
//    "BreakDuration": "00:00:30",
//    "Timeout": "00:00:30",
//    "PermitLimit": 100,
//    "QueueLimit": 0,
//    "EnableRetry": true,
//    "EnableCircuitBreaker": true,
//    "EnableTimeout": true,
//    "EnableConcurrencyLimiter": false,
//    "EnableFallback": false
//  }
//}


//public class PaymentService
//{
//    private readonly HttpClient _httpClient;

//    public PaymentService(IHttpClientFactory httpClientFactory)
//    {
//        _httpClient = httpClientFactory.CreateClient("ExternalApi");
//    }

//    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
//    {
//        // Resilience is automatically applied!
//        var response = await _httpClient.PostAsJsonAsync("/payments", request);
//        response.EnsureSuccessStatusCode();
//        return await response.Content.ReadFromJsonAsync<PaymentResult>();
//    }
//}
