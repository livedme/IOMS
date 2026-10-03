using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CustomerDtoMappingProfile : Profile
{
    public CustomerDtoMappingProfile()
    {
        CreateMap<Customer, CustomerDto>();
    }
}
