using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class LandedCostService : ILandedCostService
{
    private readonly ApplicationDbContext _context;

    public LandedCostService(ApplicationDbContext context) => _context = context;

    public async Task<List<LandedCostComponentDto>> GetComponents()
    {
        return await _context.LandedCostComponents
            .Select(c => new LandedCostComponentDto(c.Id, c.Name, c.Type, c.DefaultRate))
            .ToListAsync();
    }

    public async Task<Guid> CreateComponent(CreateLandedCostComponentDto dto)
    {
        var comp = new LandedCostComponent
        {
            Name = dto.Name,
            Type = dto.Type,
            DefaultRate = dto.DefaultRate
        };
        _context.LandedCostComponents.Add(comp);
        await _context.SaveChangesAsync();
        return comp.Id;
    }

    public async Task<List<LandedCostAllocationDto>> GetAllocations(Guid? purchaseOrderId)
    {
        var query = _context.LandedCostAllocations
            .Include(a => a.Component)
            .Include(a => a.PurchaseOrder)
            .AsQueryable();

        if (purchaseOrderId.HasValue)
            query = query.Where(a => a.PurchaseOrderId == purchaseOrderId.Value);

        return await query
            .Select(a => new LandedCostAllocationDto(
                a.Id, a.PurchaseOrderId,
                a.PurchaseOrder != null ? a.PurchaseOrder.OrderNumber : null,
                a.ComponentId, a.Component.Name, a.Amount, a.AllocationMethod))
            .ToListAsync();
    }

    public async Task<Guid> AllocateCost(CreateLandedCostAllocationDto dto)
    {
        var alloc = new LandedCostAllocation
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            ComponentId = dto.ComponentId,
            Amount = dto.Amount,
            AllocationMethod = dto.AllocationMethod
        };
        _context.LandedCostAllocations.Add(alloc);
        await _context.SaveChangesAsync();
        return alloc.Id;
    }
}

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;

    public ApprovalService(ApplicationDbContext context) => _context = context;

    public async Task<List<ApprovalWorkflowRuleDto>> GetWorkflowRules()
    {
        return await _context.ApprovalWorkflowRules
            .Select(r => new ApprovalWorkflowRuleDto(r.Id, r.DocumentType, r.Condition,
                r.AmountThreshold, r.ApproverRoleOrUserId, r.Level, r.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateWorkflowRule(CreateApprovalWorkflowRuleDto dto)
    {
        var rule = new ApprovalWorkflowRule
        {
            DocumentType = dto.DocumentType,
            Condition = dto.Condition,
            AmountThreshold = dto.AmountThreshold,
            ApproverRoleOrUserId = dto.ApproverRoleOrUserId,
            Level = dto.Level,
            IsActive = true
        };
        _context.ApprovalWorkflowRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule.Id;
    }

    public async Task DeleteWorkflowRule(Guid id)
    {
        var rule = await _context.ApprovalWorkflowRules.FindAsync(id)
            ?? throw new EntityNotFoundException("ApprovalWorkflowRule", id);
        rule.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ApprovalRequestDto>> GetPendingApprovals(string? userId, int page, int pageSize)
    {
        var query = _context.ApprovalRequests
            .Include(a => a.Steps)
            .Where(a => a.Status == ApprovalDecision.Pending)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.Steps.Any(s => s.ApproverId == userId && s.Decision == ApprovalDecision.Pending));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ApprovalRequestDto(a.Id, a.DocumentType, a.DocumentId, a.Status, a.CurrentLevel,
                a.Steps.Select(s => new ApprovalRequestStepDto(s.Id, s.ApproverId, s.Level, s.Decision, s.DecidedAt, s.Notes)).ToList()))
            .ToListAsync();

        return new PagedResult<ApprovalRequestDto>(items, total, page, pageSize);
    }

    public async Task<Guid> SubmitForApproval(string documentType, Guid documentId)
    {
        var rules = await _context.ApprovalWorkflowRules
            .Where(r => r.DocumentType == documentType && r.IsActive)
            .OrderBy(r => r.Level)
            .ToListAsync();

        var request = new ApprovalRequest
        {
            DocumentType = documentType,
            DocumentId = documentId,
            Status = ApprovalDecision.Pending,
            CurrentLevel = 1
        };

        foreach (var rule in rules)
        {
            request.Steps.Add(new ApprovalRequestStep
            {
                ApproverId = rule.ApproverRoleOrUserId ?? "Admin",
                Level = rule.Level,
                Decision = ApprovalDecision.Pending
            });
        }

        _context.ApprovalRequests.Add(request);
        await _context.SaveChangesAsync();
        return request.Id;
    }

    public async Task ProcessDecision(Guid approvalRequestStepId, ApprovalDecision decision, string? notes)
    {
        var step = await _context.ApprovalRequestSteps
            .Include(s => s.ApprovalRequest).ThenInclude(a => a.Steps)
            .FirstOrDefaultAsync(s => s.Id == approvalRequestStepId)
            ?? throw new EntityNotFoundException("ApprovalRequestStep", approvalRequestStepId);

        step.Decision = decision;
        step.DecidedAt = DateTime.UtcNow;
        step.Notes = notes;

        if (decision == ApprovalDecision.Rejected)
        {
            step.ApprovalRequest.Status = ApprovalDecision.Rejected;
        }
        else if (decision == ApprovalDecision.Approved)
        {
            var nextStep = step.ApprovalRequest.Steps
                .OrderBy(s => s.Level)
                .FirstOrDefault(s => s.Decision == ApprovalDecision.Pending && s.Id != step.Id);

            if (nextStep == null)
                step.ApprovalRequest.Status = ApprovalDecision.Approved;
            else
                step.ApprovalRequest.CurrentLevel = nextStep.Level;
        }

        await _context.SaveChangesAsync();
    }
}

public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly ApplicationDbContext _context;

    public DocumentTemplateService(ApplicationDbContext context) => _context = context;

    public async Task<List<DocumentTemplateDto>> GetTemplates()
    {
        return await _context.DocumentTemplates
            .Select(t => new DocumentTemplateDto(t.Id, t.Name, t.Type, t.HtmlContent, t.IsDefault))
            .ToListAsync();
    }

    public async Task<Guid> CreateTemplate(CreateDocumentTemplateDto dto)
    {
        var template = new DocumentTemplate
        {
            Name = dto.Name,
            Type = dto.Type,
            HtmlContent = dto.HtmlContent,
            IsDefault = dto.IsDefault
        };
        _context.DocumentTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template.Id;
    }

    public async Task UpdateTemplate(Guid id, CreateDocumentTemplateDto dto)
    {
        var t = await _context.DocumentTemplates.FindAsync(id)
            ?? throw new EntityNotFoundException("DocumentTemplate", id);
        t.Name = dto.Name;
        t.Type = dto.Type;
        t.HtmlContent = dto.HtmlContent;
        t.IsDefault = dto.IsDefault;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTemplate(Guid id)
    {
        var t = await _context.DocumentTemplates.FindAsync(id)
            ?? throw new EntityNotFoundException("DocumentTemplate", id);
        t.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}

public class WebhookService : IWebhookService
{
    private readonly ApplicationDbContext _context;

    public WebhookService(ApplicationDbContext context) => _context = context;

    public async Task<List<WebhookSubscriptionDto>> GetSubscriptions()
    {
        return await _context.WebhookSubscriptions
            .Include(w => w.DeliveryLogs)
            .Select(w => new WebhookSubscriptionDto(w.Id, w.EventType, w.Url, w.IsActive, w.DeliveryLogs.Count))
            .ToListAsync();
    }

    public async Task<Guid> CreateSubscription(CreateWebhookSubscriptionDto dto)
    {
        var sub = new WebhookSubscription
        {
            EventType = dto.EventType,
            Url = dto.Url,
            Secret = dto.Secret,
            IsActive = true
        };
        _context.WebhookSubscriptions.Add(sub);
        await _context.SaveChangesAsync();
        return sub.Id;
    }

    public async Task DeleteSubscription(Guid id)
    {
        var sub = await _context.WebhookSubscriptions.FindAsync(id)
            ?? throw new EntityNotFoundException("WebhookSubscription", id);
        sub.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<WebhookDeliveryLogDto>> GetDeliveryLogs(Guid subscriptionId)
    {
        return await _context.WebhookDeliveryLogs
            .Where(l => l.SubscriptionId == subscriptionId)
            .OrderByDescending(l => l.SentAt)
            .Select(l => new WebhookDeliveryLogDto(l.Id, l.EventType, l.StatusCode, l.Attempt, l.SentAt, l.IsSuccess))
            .ToListAsync();
    }
}

public class CustomFieldService : ICustomFieldService
{
    private readonly ApplicationDbContext _context;

    public CustomFieldService(ApplicationDbContext context) => _context = context;

    public async Task<List<CustomFieldDefinitionDto>> GetDefinitions(string? entityType)
    {
        var query = _context.CustomFieldDefinitions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(d => d.EntityType == entityType);

        return await query.OrderBy(d => d.SortOrder)
            .Select(d => new CustomFieldDefinitionDto(d.Id, d.EntityType, d.FieldName, d.FieldType, d.IsRequired, d.Options, d.SortOrder))
            .ToListAsync();
    }

    public async Task<Guid> CreateDefinition(CreateCustomFieldDefinitionDto dto)
    {
        var maxSort = await _context.CustomFieldDefinitions
            .Where(d => d.EntityType == dto.EntityType)
            .MaxAsync(d => (int?)d.SortOrder) ?? 0;

        var def = new CustomFieldDefinition
        {
            EntityType = dto.EntityType,
            FieldName = dto.FieldName,
            FieldType = dto.FieldType,
            IsRequired = dto.IsRequired,
            Options = dto.Options,
            SortOrder = maxSort + 1
        };
        _context.CustomFieldDefinitions.Add(def);
        await _context.SaveChangesAsync();
        return def.Id;
    }

    public async Task DeleteDefinition(Guid id)
    {
        var def = await _context.CustomFieldDefinitions.FindAsync(id)
            ?? throw new EntityNotFoundException("CustomFieldDefinition", id);
        def.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<CustomFieldValueDto>> GetValues(Guid entityId)
    {
        return await _context.CustomFieldValues
            .Include(v => v.Definition)
            .Where(v => v.EntityId == entityId)
            .Select(v => new CustomFieldValueDto(v.Id, v.DefinitionId, v.Definition.FieldName, v.Value))
            .ToListAsync();
    }

    public async Task SetValue(Guid definitionId, Guid entityId, string? value)
    {
        var existing = await _context.CustomFieldValues
            .FirstOrDefaultAsync(v => v.DefinitionId == definitionId && v.EntityId == entityId);

        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            _context.CustomFieldValues.Add(new CustomFieldValue
            {
                DefinitionId = definitionId,
                EntityId = entityId,
                Value = value
            });
        }

        await _context.SaveChangesAsync();
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<AuditLogDto>> GetAuditLogs(string? tableName, string? action, int page, int pageSize)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(a => a.TableName.Contains(tableName));
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.TableName, a.RecordId, a.Action,
                a.OldValues, a.NewValues, a.UserId, a.Timestamp))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, total, page, pageSize);
    }
}

public class ScheduledReportService : IScheduledReportService
{
    private readonly ApplicationDbContext _context;

    public ScheduledReportService(ApplicationDbContext context) => _context = context;

    public async Task<List<ScheduledReportDto>> GetScheduledReports()
    {
        return await _context.ScheduledReports
            .Select(r => new ScheduledReportDto(r.Id, r.ReportType, r.CronExpression,
                r.Recipients, r.Format, r.IsActive, r.LastRunAt, r.LastRunStatus))
            .ToListAsync();
    }

    public async Task<Guid> CreateScheduledReport(CreateScheduledReportDto dto)
    {
        var report = new ScheduledReport
        {
            ReportType = dto.ReportType,
            CronExpression = dto.CronExpression,
            Recipients = dto.Recipients,
            Format = dto.Format,
            IsActive = true
        };
        _context.ScheduledReports.Add(report);
        await _context.SaveChangesAsync();
        return report.Id;
    }

    public async Task UpdateScheduledReport(Guid id, CreateScheduledReportDto dto)
    {
        var r = await _context.ScheduledReports.FindAsync(id)
            ?? throw new EntityNotFoundException("ScheduledReport", id);
        r.ReportType = dto.ReportType;
        r.CronExpression = dto.CronExpression;
        r.Recipients = dto.Recipients;
        r.Format = dto.Format;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteScheduledReport(Guid id)
    {
        var r = await _context.ScheduledReports.FindAsync(id)
            ?? throw new EntityNotFoundException("ScheduledReport", id);
        r.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}

public class DataPrivacyService : IDataPrivacyService
{
    private readonly ApplicationDbContext _context;

    public DataPrivacyService(ApplicationDbContext context) => _context = context;

    public async Task<List<DataSubjectRequestDto>> GetRequests()
    {
        return await _context.DataSubjectRequests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DataSubjectRequestDto(r.Id, r.RequestType, r.SubjectEmail,
                r.Status, r.CompletedAt, r.Notes))
            .ToListAsync();
    }

    public async Task<Guid> CreateRequest(CreateDataSubjectRequestDto dto)
    {
        var request = new DataSubjectRequest
        {
            RequestType = dto.RequestType,
            SubjectEmail = dto.SubjectEmail,
            Notes = dto.Notes,
            Status = DataSubjectRequestStatus.Pending
        };
        _context.DataSubjectRequests.Add(request);
        await _context.SaveChangesAsync();
        return request.Id;
    }

    public async Task ProcessRequest(Guid id)
    {
        var r = await _context.DataSubjectRequests.FindAsync(id)
            ?? throw new EntityNotFoundException("DataSubjectRequest", id);
        r.Status = DataSubjectRequestStatus.Completed;
        r.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

public class ArchivalService : IArchivalService
{
    private readonly ApplicationDbContext _context;

    public ArchivalService(ApplicationDbContext context) => _context = context;

    public async Task<List<ArchivalPolicyDto>> GetPolicies()
    {
        return await _context.ArchivalPolicies
            .Select(p => new ArchivalPolicyDto(p.Id, p.EntityType, p.RetentionDays, p.IsActive, p.LastRunAt))
            .ToListAsync();
    }

    public async Task<Guid> CreatePolicy(CreateArchivalPolicyDto dto)
    {
        var policy = new ArchivalPolicy
        {
            EntityType = dto.EntityType,
            RetentionDays = dto.RetentionDays,
            IsActive = true
        };
        _context.ArchivalPolicies.Add(policy);
        await _context.SaveChangesAsync();
        return policy.Id;
    }

    public async Task UpdatePolicy(Guid id, CreateArchivalPolicyDto dto)
    {
        var p = await _context.ArchivalPolicies.FindAsync(id)
            ?? throw new EntityNotFoundException("ArchivalPolicy", id);
        p.EntityType = dto.EntityType;
        p.RetentionDays = dto.RetentionDays;
        await _context.SaveChangesAsync();
    }

    public async Task DeletePolicy(Guid id)
    {
        var p = await _context.ArchivalPolicies.FindAsync(id)
            ?? throw new EntityNotFoundException("ArchivalPolicy", id);
        p.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}
