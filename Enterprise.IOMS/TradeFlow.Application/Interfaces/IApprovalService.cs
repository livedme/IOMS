using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IApprovalService
{
    Task<List<ApprovalWorkflowRuleDto>> GetWorkflowRules();
    Task<Guid> CreateWorkflowRule(CreateApprovalWorkflowRuleDto dto);
    Task DeleteWorkflowRule(Guid id);
    Task<PagedResult<ApprovalRequestDto>> GetPendingApprovals(string? userId, int page, int pageSize);
    Task<Guid> SubmitForApproval(string documentType, Guid documentId);
    Task ProcessDecision(Guid approvalRequestStepId, ApprovalDecision decision, string? notes);
}
