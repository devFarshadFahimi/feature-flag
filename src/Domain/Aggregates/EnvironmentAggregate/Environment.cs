using Domain.Aggregates.ApiTokenAggregate;
using Domain.Aggregates.EnvironmentAggregate.Events;

namespace Domain.Aggregates.EnvironmentAggregate;

public sealed class Environment : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public EnvironmentType Type { get; private set; }
    public bool Enabled { get; private set; }
    public int SortOrder { get; private set; }
    public bool Protected { get; private set; }

    private readonly List<ApiToken> _tokens = [];
    public IReadOnlyCollection<ApiToken> Tokens => _tokens.AsReadOnly();

    private Environment(Guid id, string name, EnvironmentType type, int sortOrder)
    {
        Name = name;
        Type = type;
        Enabled = true;
        SortOrder = sortOrder;
    }

    public static Environment Create(string name, EnvironmentType type, int sortOrder = 0)
    {
        var env = new Environment(Guid.NewGuid(), name, type, sortOrder);
        env.Apply(new EnvironmentCreatedEvent(env.Id, name, type));
        return env;
    }

    public void Enable()
    {
        Enabled = true;
    }

    public void Disable()
    {
        Enabled = false;
    }

    public void UpdateSortOrder(int order)
    {
        SortOrder = order;
    }

    public void SetProtected(bool isProtected)
    {
        Protected = isProtected;
    }

    public ApiToken CreateToken(ApiTokenType tokenType, string? name = null, DateTime? expiresAt = null)
    {
        var token = ApiToken.Create(Id, tokenType, Guid.NewGuid().ToString(), name: name, expiresAt: expiresAt);
        _tokens.Add(token);
        return token;
    }

    public void RevokeToken(Guid tokenId)
    {
        var token = _tokens.FirstOrDefault(t => t.Id == tokenId)
            ?? throw new InvalidEntityStateException($"Token {tokenId} not found");

        token.Revoke();
        _tokens.Remove(token);
    }
}