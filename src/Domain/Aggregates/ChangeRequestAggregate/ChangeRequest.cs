using Domain.Aggregates.ChangeRequestAggregate.Events;
using Domain.Aggregates.ProjectAggregate;

namespace Domain.Aggregates.ChangeRequestAggregate;

public sealed class ChangeRequest : AggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    public Guid EnvironmentId { get; private set; }
    public Environment Environment { get; private set; }

    public Guid CreatedBy { get; private set; }
    public ChangeRequestStatus Status { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }

    public ICollection<ChangeRequestItem> Items { get; private set; } = [];

    public List<Guid> Reviewers { get; private set; } = [];

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

        cr.AddEvent(new ChangeRequestCreatedEvent(cr.Id, projectId, environmentId));
        return cr;
    }

    public void AddItem(ChangeRequestItem item)
    {
        Items.Add(item);
    }

    public void RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidEntityStateException($"Item {itemId} not found");

        _ = Items.Remove(item);
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
        if (!Reviewers.Contains(reviewerId))
        {
            Reviewers.Add(reviewerId);
        }
    }
}
