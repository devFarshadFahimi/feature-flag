namespace Application.Common.DTOs;

public sealed record SelectiveDTO<TKey>(TKey Key, string Value);
