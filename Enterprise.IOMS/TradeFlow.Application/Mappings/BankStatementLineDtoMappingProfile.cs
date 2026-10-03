using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class BankStatementLineDtoMappingProfile : Profile
{
    public BankStatementLineDtoMappingProfile()
    {
        CreateMap<BankStatementLine, BankStatementLineDto>();
    }
}
