using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ArchivalPolicyDtoMappingProfile : Profile
{
    public ArchivalPolicyDtoMappingProfile()
    {
        CreateMap<ArchivalPolicy, ArchivalPolicyDto>();
    }
}
