using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class RfqRequestDtoMappingProfile : Profile
{
    public RfqRequestDtoMappingProfile()
    {
        CreateMap<RfqRequest, RfqRequestDto>();
    }
}
