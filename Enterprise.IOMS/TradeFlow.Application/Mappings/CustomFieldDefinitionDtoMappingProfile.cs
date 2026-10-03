using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CustomFieldDefinitionDtoMappingProfile : Profile
{
    public CustomFieldDefinitionDtoMappingProfile()
    {
        CreateMap<CustomFieldDefinition, CustomFieldDefinitionDto>();
    }
}
