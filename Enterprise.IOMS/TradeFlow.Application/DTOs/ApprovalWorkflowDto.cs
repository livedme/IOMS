using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record ApprovalWorkflowRuleDto(Guid Id, string DocumentType, string? Condition,
    decimal? AmountThreshold, string? ApproverRoleOrUserId, int Level, bool IsActive);
public record CreateApprovalWorkflowRuleDto(string DocumentType, string? Condition,
    decimal? AmountThreshold, string? ApproverRoleOrUserId, int Level);
public record ApprovalRequestDto(Guid Id, string DocumentType, Guid DocumentId,
    ApprovalDecision Status, int CurrentLevel, List<ApprovalRequestStepDto> Steps);
public record ApprovalRequestStepDto(Guid Id, string ApproverId, int Level,
    ApprovalDecision Decision, DateTime? DecidedAt, string? Notes);
