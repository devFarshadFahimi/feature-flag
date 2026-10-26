using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Configurations;

internal sealed class JsonValueComparer<T>() : ValueComparer<T>((left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null), value => JsonSerializer
    .Serialize(value, (JsonSerializerOptions?)null)
    .GetHashCode(StringComparison.Ordinal), value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);