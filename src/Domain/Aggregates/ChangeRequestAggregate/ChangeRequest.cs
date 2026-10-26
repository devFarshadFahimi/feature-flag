using Domain.Aggregates.ChangeRequestAggregate.Events;
using Domain.Aggregates.ProjectAggregate;

namespace Domain.Aggregates.ChangeRequestAggregate;

public sealed class ChangeRequest : AggregateRoot<Guid>
{
    public long Revision
    {
        get; private set;
    }
    public Guid? ProjectId
    {
        get; private set;
    }
    public string Scope => ProjectId.HasValue ? "Project" : "Global";
    public string? RejectionReason
    {
        get; private set;
    }
    public int RequiredApprovals { get; private set; } = 1;
    public Dictionary<string, long> CapturedRevisions { get; private set; } = [];
    public string? ScopeFingerprint
    {
        get; private set;
    }
    public Guid? ScheduledBy
    {
        get; private set;
    }
    public Guid? PredecessorId
    {
        get; private set;
    }
    public Dictionary<string, long> ResultRevisions { get; private set; } = [];
    public int Attempts
    {
        get; private set;
    }
    public string? FailureReason
    {
        get; private set;
    }
    public DateTime? NextAttemptAt
    {
        get; private set;
    }
    public Project Project { get; private set; } = null!;
    public Guid EnvironmentId
    {
        get; private set;
    }
    public Environment Environment
    {
        get; private set;
    }
    public Guid CreatedBy
    {
        get; private set;
    }
    public ChangeRequestStatus Status
    {
        get; private set;
    }
    public string? Title
    {
        get; private set;
    }
    public string? Description
    {
        get; private set;
    }
    public DateTime? ScheduledAt
    {
        get; private set;
    }
    public DateTime CreatedAt
    {
        get; private set;
    }
    public DateTime? ReviewedAt
    {
        get; private set;
    }
    public Guid? ReviewedBy
    {
        get; private set;
    }
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
        if (Status is ChangeRequestStatus.Applied or ChangeRequestStatus.Cancelled)
        {
            throw new InvalidEntityStateException("درخواست نهایی‌شده قابل ویرایش نیست.");
        }


        Status = ChangeRequestStatus.Draft;
        Reviewers.Clear();
        ReviewedBy = null;
        ReviewedAt = null;
        CapturedRevisions.Clear();
        ScopeFingerprint = null;
        Items.Add(item);
        Revision++;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId) ?? throw new InvalidEntityStateException($"قلم تغییر {itemId} یافت نشد.");

        _ = Items.Remove(item);
    }

    public void SubmitForReview()
    {
        if (Status != ChangeRequestStatus.Draft)
        {
            throw new InvalidEntityStateException("فقط پیش‌نویس را می‌توان برای بررسی ارسال کرد.");
        }

        if (Items.Count == 0)
        {
            throw new InvalidEntityStateException("درخواست تغییر باید حداقل یک قلم داشته باشد.");
        }


        Status = ChangeRequestStatus.InReview;
    }

    public void Approve(Guid reviewerId)
    {
        if (Status != ChangeRequestStatus.InReview)
        {
            throw new InvalidEntityStateException("فقط درخواست در حال بررسی را می‌توان تأیید کرد.");
        }

        if (reviewerId == CreatedBy)
        {
            throw new InvalidEntityStateException("تأیید درخواست تغییر خودتان مجاز نیست.");
        }

        if (Reviewers.Contains(reviewerId))
        {
            throw new InvalidEntityStateException("این کاربر قبلاً درخواست را تأیید کرده است.");
        }


        Reviewers.Add(reviewerId);
        Status = Reviewers.Count >= RequiredApprovals ? ChangeRequestStatus.Approved : ChangeRequestStatus.InReview;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(Guid reviewerId, string? reason = null)
    {
        if (Status != ChangeRequestStatus.InReview)
        {
            throw new InvalidEntityStateException("فقط درخواست در حال بررسی را می‌توان رد کرد.");
        }


        Status = ChangeRequestStatus.Rejected;
        RejectionReason = reason;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Apply()
    {
        if (Status is not ChangeRequestStatus.Approved and not ChangeRequestStatus.Scheduled)
        {
            throw new InvalidEntityStateException("فقط درخواست تأییدشده یا زمان‌بندی‌شده قابل اعمال است.");
        }


        Status = ChangeRequestStatus.Applied;
    }

    public void Schedule(DateTime scheduledAt)
    {
        if (Status != ChangeRequestStatus.Approved)
        {
            throw new InvalidEntityStateException("فقط درخواست تأییدشده قابل زمان‌بندی است.");
        }

        if (scheduledAt.Kind != DateTimeKind.Utc || scheduledAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("زمان اجرا باید در آینده و بر مبنای زمان جهانی باشد.");
        }


        ScheduledAt = scheduledAt;
        Status = ChangeRequestStatus.Scheduled;
    }

    public void Cancel()
    {
        if (Status == ChangeRequestStatus.Applied)
        {
            throw new InvalidEntityStateException("لغو درخواست اعمال‌شده مجاز نیست.");
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

    public static ChangeRequest CreateGlobal(Guid environmentId, Guid createdBy, string? title, string? description)
    {
        var request = Create(Guid.Empty, environmentId, createdBy, title, description);

        request.ProjectId = null;

        return request;
    }

    public void Capture(int required, Dictionary<string, long> revisions, string? fingerprint)
    {
        RequiredApprovals = required;
        CapturedRevisions = revisions;
        ScopeFingerprint = fingerprint;
    }

    public void SetPredecessor(Guid? predecessor)
    {
        if (Status != ChangeRequestStatus.Draft)
        {
            throw new InvalidEntityStateException("مرحله پیشین فقط پیش از بررسی قابل تعیین است.");
        }


        PredecessorId = predecessor;
    }

    public void SetScheduleActor(Guid actor, Guid? predecessor)
    {
        if (predecessor != PredecessorId)
        {
            throw new InvalidEntityStateException("مرحله پیشین پس از تأیید قابل تغییر نیست.");
        }


        ScheduledBy = actor;
    }

    public void RecordResult(Dictionary<string, long> revisions) => ResultRevisions = revisions;
    public void Suspend(string reason)
    {
        Status = ChangeRequestStatus.Suspended;
        FailureReason = reason;
        Reviewers.Clear();
    }

    public void Retry(string reason)
    {
        Attempts++;
        FailureReason = reason;

        if (Attempts >= 5)
        {
            Status = ChangeRequestStatus.Failed;
        }
        else
        {
            NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, Attempts) * 5);
        }
    }
}