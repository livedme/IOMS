using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ApprovalRequestStepDtoMappingProfile : Profile
{
    public ApprovalRequestStepDtoMappingProfile()
    {
        CreateMap<ApprovalRequestStep, ApprovalRequestStepDto>();
    }
}
