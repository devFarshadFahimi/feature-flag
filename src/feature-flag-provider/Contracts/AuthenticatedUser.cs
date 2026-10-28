namespace FeatureFlag.Provider.Contracts;

public sealed record AuthenticatedUser(Guid Id, string Email, string? Name, string Role);
