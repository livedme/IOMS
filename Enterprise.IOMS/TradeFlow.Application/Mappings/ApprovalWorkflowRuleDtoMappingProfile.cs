using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ApprovalWorkflowRuleDtoMappingProfile : Profile
{
    public ApprovalWorkflowRuleDtoMappingProfile()
    {
        CreateMap<ApprovalWorkflowRule, ApprovalWorkflowRuleDto>();
    }
}
