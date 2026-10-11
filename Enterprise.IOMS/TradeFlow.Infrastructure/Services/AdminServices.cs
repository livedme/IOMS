using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class LandedCostService : ILandedCostService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public LandedCostService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<LandedCostComponentDto>> GetComponents()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.LandedCostComponents
            .Select(c => new LandedCostComponentDto(c.Id, c.Name, c.Type, c.DefaultRate))
            .ToListAsync();
    }

    public async Task<Guid> CreateComponent(CreateLandedCostComponentDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public ApprovalService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ApprovalWorkflowRuleDto>> GetWorkflowRules()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.ApprovalWorkflowRules
            .Select(r => new ApprovalWorkflowRuleDto(r.Id, r.DocumentType, r.Condition,
                r.AmountThreshold, r.ApproverRoleOrUserId, r.Level, r.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateWorkflowRule(CreateApprovalWorkflowRuleDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var rule = await _context.ApprovalWorkflowRules.FindAsync(id)
            ?? throw new EntityNotFoundException("ApprovalWorkflowRule", id);
        rule.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ApprovalRequestDto>> GetPendingApprovals(string? userId, int page, int pageSize)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DocumentTemplateService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<DocumentTemplateDto>> GetTemplates()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.DocumentTemplates
            .Select(t => new DocumentTemplateDto(t.Id, t.Name, t.Type, t.HtmlContent, t.IsDefault))
            .ToListAsync();
    }

    public async Task<Guid> CreateTemplate(CreateDocumentTemplateDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var t = await _context.DocumentTemplates.FindAsync(id)
            ?? throw new EntityNotFoundException("DocumentTemplate", id);
        t.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}

public class WebhookService : IWebhookService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public WebhookService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<WebhookSubscriptionDto>> GetSubscriptions()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.WebhookSubscriptions
            .Include(w => w.DeliveryLogs)
            .Select(w => new WebhookSubscriptionDto(w.Id, w.EventType, w.Url, w.IsActive, w.DeliveryLogs.Count))
            .ToListAsync();
    }

    public async Task<Guid> CreateSubscription(CreateWebhookSubscriptionDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var sub = await _context.WebhookSubscriptions.FindAsync(id)
            ?? throw new EntityNotFoundException("WebhookSubscription", id);
        sub.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<WebhookDeliveryLogDto>> GetDeliveryLogs(Guid subscriptionId)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.WebhookDeliveryLogs
            .Where(l => l.SubscriptionId == subscriptionId)
            .OrderByDescending(l => l.SentAt)
            .Select(l => new WebhookDeliveryLogDto(l.Id, l.EventType, l.StatusCode, l.Attempt, l.SentAt, l.IsSuccess))
            .ToListAsync();
    }
}

public class CustomFieldService : ICustomFieldService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public CustomFieldService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<CustomFieldDefinitionDto>> GetDefinitions(string? entityType)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var query = _context.CustomFieldDefinitions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(d => d.EntityType == entityType);

        return await query.OrderBy(d => d.SortOrder)
            .Select(d => new CustomFieldDefinitionDto(d.Id, d.EntityType, d.FieldName, d.FieldType, d.IsRequired, d.Options, d.SortOrder))
            .ToListAsync();
    }

    public async Task<Guid> CreateDefinition(CreateCustomFieldDefinitionDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var def = await _context.CustomFieldDefinitions.FindAsync(id)
            ?? throw new EntityNotFoundException("CustomFieldDefinition", id);
        def.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<CustomFieldValueDto>> GetValues(Guid entityId)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.CustomFieldValues
            .Include(v => v.Definition)
            .Where(v => v.EntityId == entityId)
            .Select(v => new CustomFieldValueDto(v.Id, v.DefinitionId, v.Definition.FieldName, v.Value))
            .ToListAsync();
    }

    public async Task SetValue(Guid definitionId, Guid entityId, string? value)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public AuditLogService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogs(string? tableName, string? action, int page, int pageSize)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public ScheduledReportService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ScheduledReportDto>> GetScheduledReports()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.ScheduledReports
            .Select(r => new ScheduledReportDto(r.Id, r.ReportType, r.CronExpression,
                r.Recipients, r.Format, r.IsActive, r.LastRunAt, r.LastRunStatus))
            .ToListAsync();
    }

    public async Task<Guid> CreateScheduledReport(CreateScheduledReportDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var r = await _context.ScheduledReports.FindAsync(id)
            ?? throw new EntityNotFoundException("ScheduledReport", id);
        r.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}

public class DataPrivacyService : IDataPrivacyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DataPrivacyService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<DataSubjectRequestDto>> GetRequests()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.DataSubjectRequests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DataSubjectRequestDto(r.Id, r.RequestType, r.SubjectEmail,
                r.Status, r.CompletedAt, r.Notes))
            .ToListAsync();
    }

    public async Task<Guid> CreateRequest(CreateDataSubjectRequestDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var r = await _context.DataSubjectRequests.FindAsync(id)
            ?? throw new EntityNotFoundException("DataSubjectRequest", id);
        r.Status = DataSubjectRequestStatus.Completed;
        r.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

public class ArchivalService : IArchivalService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public ArchivalService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ArchivalPolicyDto>> GetPolicies()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.ArchivalPolicies
            .Select(p => new ArchivalPolicyDto(p.Id, p.EntityType, p.RetentionDays, p.IsActive, p.LastRunAt))
            .ToListAsync();
    }

    public async Task<Guid> CreatePolicy(CreateArchivalPolicyDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
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
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var p = await _context.ArchivalPolicies.FindAsync(id)
            ?? throw new EntityNotFoundException("ArchivalPolicy", id);
        p.EntityType = dto.EntityType;
        p.RetentionDays = dto.RetentionDays;
        await _context.SaveChangesAsync();
    }

    public async Task DeletePolicy(Guid id)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var p = await _context.ArchivalPolicies.FindAsync(id)
            ?? throw new EntityNotFoundException("ArchivalPolicy", id);
        p.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}
