namespace Domain.Aggregates.HistoryAggregate;
// PostgreSQL statement triggers advance this value atomically with every configuration write.
public sealed class ConfigurationVersion
{
    public int Id
    {
        get; set;
    }

    public long Revision
    {
        get; set;
    }
}