using System.Text.Json;

namespace FeatureFlag.Provider.Contracts;

public sealed record EvaluatedVariant(string Name, bool Enabled, IReadOnlyDictionary<string, JsonElement>? Payload);
