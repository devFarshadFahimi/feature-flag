using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

// Run with FeatureFlags__BaseUrl, FeatureFlags__ProjectId and FeatureFlags__EnvironmentId.
// Forward a user JWT accepted by feature-flag. In an existing app, obtain it from that app's
// authenticated server session rather than introducing an application credential or browser SDK.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("flags", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FeatureFlags:BaseUrl"] ?? "http://localhost:5098/");
    client.Timeout = TimeSpan.FromSeconds(2);
});
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new { message = "سرویس ارزیابی موقتاً در دسترس نیست." });
}));

app.MapGet("/features", async (HttpContext context, IHttpClientFactory clients, IConfiguration configuration) =>
{
    context.Response.Headers.CacheControl = "no-store";

    if (!AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization) || authorization.Scheme != "Bearer")
    {
        return Results.Json(new { message = "احراز هویت الزامی است." }, statusCode: 401);
    }

    if (!Guid.TryParse(configuration["FeatureFlags:ProjectId"], out var projectId) || !Guid.TryParse(configuration["FeatureFlags:EnvironmentId"], out var environmentId))
    {
        return Results.Json(new { message = "تنظیمات پروژه یا محیط معتبر نیست." }, statusCode: 503);
    }


    var client = clients.CreateClient("flags");

    client.DefaultRequestHeaders.Authorization = authorization;
    // The API validates the issuer, audience, signature and current session. Never trust an unverified JWT payload.
    using var identity = await client.GetAsync("api/v1/Auth/me", context.RequestAborted);

    if (!identity.IsSuccessStatusCode)
    {
        return Results.Json(new { message = "نشست کاربر معتبر نیست." }, statusCode: (int)identity.StatusCode);
    }


    using var user = await JsonDocument.ParseAsync(await identity.Content.ReadAsStreamAsync(context.RequestAborted), cancellationToken: context.RequestAborted);
    var names = context.Request.Query["name"]
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .ToArray();

    if (names.Length is < 1 or > 100)
    {
        return Results.Json(new { message = "بین ۱ تا ۱۰۰ نام پرچم ارسال کنید." }, statusCode: 400);
    }


    var body = new
    {
        projectId,
        environmentId,
        appName = "backend-consumer-example",
        context = new
        {
            userId = user.RootElement
        .GetProperty("id")
        .GetString(),
            remoteAddress = context.Connection.RemoteIpAddress?.ToString()
        },
        featureNames = names,
        defaults = names.ToDictionary(n => n!, _ => false)
    };
    // Configuration caching happens in feature-flag. Do not cache decisions here: time constraints
    // and random strategies must be reevaluated. Retry only transient failures, never permissions.
    for (var attempt = 0; attempt < 2; attempt++)
    {
        try
        {
            using var response = await client.PostAsJsonAsync("api/v1/Evaluations", body, context.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                var evaluation = await response.Content.ReadFromJsonAsync<JsonElement>(context.RequestAborted);

                return Results.Json(new { features = evaluation.GetProperty("features"), fallback = false });
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return Results.Json(new { message = "دسترسی به پرچم‌های پروژه مجاز نیست." }, statusCode: (int)response.StatusCode);
            }

            if ((int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return Results.Json(new { message = "درخواست ارزیابی معتبر نیست." }, statusCode: (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException)when (!context.RequestAborted.IsCancellationRequested)
        {
        // A local request timeout permits bounded retry; caller cancellation propagates.
        }
        catch (HttpRequestException)
        {
        // The explicit defaults below remain distinguishable from authoritative decisions.
        }


        if (attempt == 0)
        {
            await Task.Delay(Random.Shared.Next(80, 180), context.RequestAborted);
        }
    }


    return Results.Json(new { features = names.Select(name => new { name, enabled = false, variant = new { name = "disabled", enabled = false, payload = (object? )null }, reason = "consumer_default" }), fallback = true });
});
app.Run();
