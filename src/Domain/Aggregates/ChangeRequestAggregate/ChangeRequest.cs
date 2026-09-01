using Domain.Aggregates.ChangeRequestAggregate.Events;

namespace Domain.Aggregates.ChangeRequestAggregate;

public sealed class ChangeRequest : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public ChangeRequestStatus Status { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }

    private readonly List<ChangeRequestItem> _items = [];
    public IReadOnlyCollection<ChangeRequestItem> Items => _items.AsReadOnly();

    private readonly List<Guid> _reviewers = [];
    public IReadOnlyCollection<Guid> Reviewers => _reviewers.AsReadOnly();

    private ChangeRequest()
    {
    }

    public static ChangeRequest Create(Guid projectId, Guid environmentId, Guid createdBy, string? title = null, string? description = null)
    {
        var cr = new ChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EnvironmentId = environmentId,
            CreatedBy = createdBy,
            Status = ChangeRequestStatus.Draft,
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        cr.Apply(new ChangeRequestCreatedEvent(cr.Id, projectId, environmentId));
        return cr;
    }

    public void AddItem(ChangeRequestItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidEntityStateException($"Item {itemId} not found");

        _items.Remove(item);
    }

    public void SubmitForReview()
    {
        if (Status != ChangeRequestStatus.Draft)
        {
            throw new InvalidEntityStateException("Can only submit draft change requests");
        }

        Status = ChangeRequestStatus.InReview;
    }

    public void Approve(Guid reviewerId)
    {
        if (Status != ChangeRequestStatus.InReview)
        {
            throw new InvalidEntityStateException("Can only approve change requests in review");
        }

        if (reviewerId == CreatedBy)
        {
            throw new InvalidEntityStateException("Cannot approve your own change request");
        }

        Status = ChangeRequestStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(Guid reviewerId, string? reason = null)
    {
        if (Status != ChangeRequestStatus.InReview)
        {
            throw new InvalidEntityStateException("Can only reject change requests in review");
        }

        Status = ChangeRequestStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Apply()
    {
        if (Status is not ChangeRequestStatus.Approved and not ChangeRequestStatus.Scheduled)
        {
            throw new InvalidEntityStateException("Can only apply approved or scheduled change requests");
        }

        Status = ChangeRequestStatus.Applied;
    }

    public void Schedule(DateTime scheduledAt)
    {
        if (Status != ChangeRequestStatus.Approved)
        {
            throw new InvalidEntityStateException("Can only schedule approved change requests");
        }

        ScheduledAt = scheduledAt;
        Status = ChangeRequestStatus.Scheduled;
    }

    public void Cancel()
    {
        if (Status == ChangeRequestStatus.Applied)
        {
            throw new InvalidEntityStateException("Cannot cancel applied change requests");
        }

        Status = ChangeRequestStatus.Cancelled;
    }

    public void AddReviewer(Guid reviewerId)
    {
        if (!_reviewers.Contains(reviewerId))
        {
            _reviewers.Add(reviewerId);
        }
    }
}

public sealed class ChangeRequestItem : Entity<Guid>
{
    public Guid ChangeRequestId { get; private set; }
    public string Action { get; private set; }
    public Guid? FeatureId { get; private set; }
    public string? Payload { get; private set; }

    private ChangeRequestItem()
    {
    }

    public static ChangeRequestItem Create(string action, Guid? featureId = null, string? payload = null)
    {
        return new ChangeRequestItem
        {
            Id = Guid.NewGuid(),
            Action = action,
            FeatureId = featureId,
            Payload = payload
        };
    }
}