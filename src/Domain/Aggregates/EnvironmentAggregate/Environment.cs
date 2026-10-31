using Domain.Aggregates.ChangeRequestAggregate;
using Domain.Aggregates.EnvironmentAggregate.Events;

namespace Domain.Aggregates.EnvironmentAggregate;

public sealed class Environment : AggregateRoot<Guid>
{
    public long Revision
    {
        get; private set;
    }

    public string Name
    {
        get; private set;
    }

    public EnvironmentType Type
    {
        get; private set;
    }

    public bool Enabled
    {
        get; private set;
    }

    public int SortOrder
    {
        get; private set;
    }

    public bool Protected
    {
        get; private set;
    }

    public ICollection<ChangeRequest> ChangeRequests { get; private set; } = [];

    private Environment(Guid id, string name, EnvironmentType type, int sortOrder)
    {
        Id = id;
        Name = name;
        Type = type;
        Enabled = true;
        SortOrder = sortOrder;
    }

    public static Environment Create(string name, EnvironmentType type, int sortOrder = 0)
    {
        var env = new Environment(Guid.NewGuid(), name, type, sortOrder);

        env.AddEvent(new EnvironmentCreatedEvent(env.Id, name, type));

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

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128)
        {
            throw new ArgumentException("نام محیط باید بین ۱ تا ۱۲۸ نویسه باشد.");
        }

        Name = name.Trim();
    }

    public void SetProtected(bool isProtected)
    {
        Protected = isProtected;
    }

}
