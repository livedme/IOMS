using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class BranchDtoMappingProfile : Profile
{
    public BranchDtoMappingProfile()
    {
        CreateMap<Branch, BranchDto>();
    }
}
