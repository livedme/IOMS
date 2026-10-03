using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class BankStatementDtoMappingProfile : Profile
{
    public BankStatementDtoMappingProfile()
    {
        CreateMap<BankStatement, BankStatementDto>();
    }
}
