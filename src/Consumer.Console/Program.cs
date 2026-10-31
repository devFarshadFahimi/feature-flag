using System.Net;
using System.Text;
using System.Text.Json;
using FeatureFlag.Provider;
using FeatureFlag.Provider.Contracts;
using FeatureFlag.Provider.Options;
using FeatureFlag.Provider.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

Console.OutputEncoding = Encoding.UTF8;

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    // Environment variables override JSON, for example FeatureFlagProvider__Password.
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();

    using var configurationLifetime = configuration as IDisposable;

    var request = ReadEvaluationRequest(configuration.GetSection("Evaluation"));
    var showFeatureTypes = true; //args.Contains("--feature-types", StringComparer.Ordinal);
    var exampleSettings = configuration.GetSection("FeatureTypeExamples");

    if (showFeatureTypes)
    {
        request = request with { FeatureNames = FeatureTypeExamples.GetFeatureNames(exampleSettings) };
    }

    var services = new ServiceCollection();

    services.AddFeatureFlagProvider(configuration.GetSection(FeatureFlagProviderOptions.SectionName));

    using var container = services.BuildServiceProvider();

    // Keep this same typed-client instance: login stores its session for evaluation.
    var provider = container.GetRequiredService<FeatureFlagProvider>();

    Console.WriteLine("1. Requesting an access token...");

    var session = await provider.IssueTokenAsync(cancellation.Token);

    // Never print or persist access/refresh tokens or the configured password.
    Console.WriteLine($"Login successful; access token expires at {session.AccessTokenExpiration:O}");
    Console.WriteLine("2. Evaluating flags with the issued token...");

    var response = await provider.EvaluateAsync(request, cancellation.Token);

    Console.WriteLine($"Evaluated at: {response.EvaluatedAt:O}");
    Console.WriteLine($"Configuration loaded at: {response.SnapshotLoadedAt:O}");
    Console.WriteLine($"Stale configuration: {response.Stale}; refresh after {response.RefreshAfterSeconds} seconds");

    foreach (var feature in response.Features)
    {
        Console.WriteLine($"{feature.Name}: enabled={feature.Enabled}, reason={feature.Reason}, revision={feature.Revision}");

        if (feature.Reason == "not_found")
        {
            Console.WriteLine("Flag not found; check the feature name and project. The default value is disabled.");
            continue;
        }

        if (showFeatureTypes)
        {
            // The specialized examples below define what true/false means for each use case.
            continue;
        }

        // Replace these branches with the consuming application's real feature behavior.
        if (feature.Enabled)
        {
            Console.WriteLine("Running the new feature path.");
        }
        else
        {
            Console.WriteLine("Running the existing application path.");
        }

        if (feature.Variant.Enabled)
        {
            Console.WriteLine($"Selected variant: {feature.Variant.Name}");

            if (feature.Variant.Payload is not null)
            {
                Console.WriteLine($"Variant payload: {JsonSerializer.Serialize(feature.Variant.Payload)}");
            }
        }
    }

    if (response.Features.Count == 0)
    {
        Console.WriteLine("No flags were returned; check the feature names and project settings.");
    }

    if (showFeatureTypes)
    {
        // This boolean simulates an existing application permission, independently of flag targeting.
        // A real backend must obtain it from its trusted authorization/entitlement system.
        var userHasReportPermission = configuration.GetValue<bool>("DemoUser:HasReportPermission");

        FeatureTypeExamples.Run(response.Features, exampleSettings, userHasReportPermission);
    }

    if (!Console.IsInputRedirected)
    {
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("The request was canceled.");
    return 130;
}
catch (HttpRequestException exception)
{
    var message = exception.StatusCode switch
    {
        HttpStatusCode.Unauthorized => "Login failed or the token is invalid; check the username and password.",
        HttpStatusCode.Forbidden => "The user does not have permission to access this project or evaluate its flags.",
        HttpStatusCode.NotFound => "The project, environment, or service endpoint was not found.",
        _ => "The feature flag service request failed; check the service URL, connection, and request settings."
    };

    // A failed HTTP request is an application error, never an authoritative disabled flag.
    Console.Error.WriteLine(message);
    return 1;
}
catch (Exception exception) when (exception is OptionsValidationException or ArgumentException or InvalidOperationException or IOException or FormatException)
{
    Console.Error.WriteLine("Invalid configuration; check the service URL, username, password, project ID, and environment ID.");
    Console.Error.WriteLine("Set the password using the FeatureFlagProvider__Password environment variable; do not store it in the sample file.");
    return 1;
}
// Builds an evaluation; the targeting user is separate from the account used to log in.
static EvaluationRequest ReadEvaluationRequest(IConfiguration settings)
{
    var appName = settings["AppName"];

    if (!Guid.TryParse(settings["ProjectId"], out var projectId) || projectId == Guid.Empty
        || !Guid.TryParse(settings["EnvironmentId"], out var environmentId) || environmentId == Guid.Empty
        || string.IsNullOrWhiteSpace(appName))
    {
        throw new ArgumentException("A valid project ID, environment ID, and application name are required.");
    }

    var context = settings.GetSection("Context");

    // Use feature names, never feature IDs. Null selects all active flags (server limit: 1,000).
    // An empty array selects none; specify at most 100 names for a batch.
    var featureNames = settings.GetSection("FeatureNames").Get<string[]>();

    return new EvaluationRequest(
        projectId,
        environmentId,
        appName,
        new EvaluationContext(
            UserId: context["UserId"],
            SessionId: context["SessionId"],
            Properties: context.GetSection("Properties").Get<Dictionary<string, string>>()),
        FeatureNames: featureNames);
}
