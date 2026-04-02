using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

public class ApprovalWorkflowRule : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public decimal? AmountThreshold { get; set; }
    public string? ApproverRoleOrUserId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ApprovalRequest : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public ApprovalDecision Status { get; set; } = ApprovalDecision.Pending;
    public int CurrentLevel { get; set; }
    public ICollection<ApprovalRequestStep> Steps { get; set; } = new List<ApprovalRequestStep>();
}

public class ApprovalRequestStep : BaseEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;
    public string ApproverId { get; set; } = string.Empty;
    public int Level { get; set; }
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public DateTime? DecidedAt { get; set; }
    public string? Notes { get; set; }
}

public class DocumentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class NotificationTemplate : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

public class NotificationLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string? Status { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WebhookSubscription : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}

public class WebhookDeliveryLog : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public WebhookSubscription Subscription { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public int? StatusCode { get; set; }
    public int Attempt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
}

public class CustomFieldDefinition : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public ICollection<CustomFieldValue> Values { get; set; } = new List<CustomFieldValue>();
}

public class CustomFieldValue : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public CustomFieldDefinition Definition { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string? Value { get; set; }
}

public class DataSubjectRequest : BaseEntity
{
    public DataSubjectRequestType RequestType { get; set; }
    public string SubjectEmail { get; set; } = string.Empty;
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class ArchivalPolicy : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
}

public class ScheduledReport : BaseEntity
{
    public string ReportType { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public string? Recipients { get; set; }
    public string? Format { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
