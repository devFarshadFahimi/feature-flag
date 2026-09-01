using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class Schedule : BaseValueObject<Schedule>
{
    public DateTime? StartDate { get; }
    public DateTime? EndDate { get; }

    private Schedule(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        StartDate = startDate;
        EndDate = endDate;
    }

    public static Schedule Create(DateTime? startDate = null, DateTime? endDate = null) =>
        new(startDate, endDate);

    public bool IsActive(DateTime now) =>
        (!StartDate.HasValue || now >= StartDate) &&
        (!EndDate.HasValue || now <= EndDate);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate ?? DateTime.MinValue;
        yield return EndDate ?? DateTime.MaxValue;
    }
}