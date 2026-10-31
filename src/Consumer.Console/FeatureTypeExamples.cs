using FeatureFlag.Provider.Contracts;
using Microsoft.Extensions.Configuration;

// Feature types describe intent in the management API. Evaluation returns decisions, not feature types.
// Configure each name below to reference a flag with that type; no metadata discovery call is required.
internal static class FeatureTypeExamples
{
    private const string Release = "Release";
    private const string Experiment = "Experiment";
    private const string Operational = "Operational";
    private const string KillSwitch = "KillSwitch";
    private const string Permission = "Permission";
    private const string NotFound = "not_found";

    private static readonly string[] Types = [Release, Experiment, Operational, KillSwitch, Permission];

    public static IReadOnlyList<string> GetFeatureNames(IConfiguration settings)
    {
        var names = Types
            .Select(type => settings[type])
            .ToArray();

        if (names.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A feature name for each of the five types is required in the FeatureTypeExamples section.");
        }

        return names
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Demonstrates consumer policy; a missing decision never executes an example action.</summary>
    /// <param name="decisions">Evaluated flags returned by the provider.</param>
    /// <param name="settings">The mapping of example types to configured flag names.</param>
    /// <param name="userHasReportPermission">A simulated permission from the consuming application.</param>
    public static void Run(IReadOnlyList<FlagDecision> decisions, IConfiguration settings, bool userHasReportPermission)
    {
        Console.WriteLine("Consumer behavior examples for the five feature types:");

        foreach (var type in Types)
        {
            var name = settings[type];
            var decision = decisions.FirstOrDefault(feature => feature.Name == name);

            if (decision is null || decision.Reason == NotFound)
            {
                Console.WriteLine($"{type}: Flag {name} was not found; the example action will not run. Configure the flag first.");
                continue;
            }

            switch (type)
            {
                case Release:
                    // Temporary rollout: retain the previous implementation until the new one is released.
                    Console.WriteLine(decision.Enabled
                        ? "Release: Displaying the new checkout."
                        : "Release: Displaying the existing checkout.");
                    break;

                case Experiment:
                    // Configure sticky control/treatment variants; flag type alone creates no A/B experiment.
                    // Assignment is not a conversion event: record business outcomes in your analytics system.
                    var useTreatment = decision.Enabled && decision.Variant.Enabled && decision.Variant.Name == "treatment";

                    Console.WriteLine(useTreatment
                        ? "Experiment: Displaying the treatment version of the recommendations page."
                        : "Experiment: Displaying the control version of the recommendations page.");
                    break;

                case Operational:
                    // A runtime workload toggle, independent of releasing new product behavior.
                    Console.WriteLine(decision.Enabled
                        ? "Operational: Running additional recommendation calculations."
                        : "Operational: Skipping optional calculations to reduce load.");
                    break;

                case KillSwitch:
                    // Convention for this example: true means STOP payment calls, false means ALLOW them.
                    // The server does not invert KillSwitch decisions. Missing/failed evaluation executes no action.
                    Console.WriteLine(decision.Enabled
                        ? "KillSwitch: Emergency stop is active; external payments are blocked."
                        : "KillSwitch: Emergency stop is inactive; the normal payment path is allowed.");
                    break;

                case Permission:
                    // Targeting context and a feature flag are not proof of the user's actual permissions.
                    Console.WriteLine(decision.Enabled && userHasReportPermission
                        ? "Permission: Both the flag and user permission are enabled; displaying advanced reports."
                        : "Permission: Advanced reports are hidden; the flag or user permission is not enabled.");
                    break;
            }
        }
    }
}
