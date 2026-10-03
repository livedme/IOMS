using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CustomFieldValueDtoMappingProfile : Profile
{
    public CustomFieldValueDtoMappingProfile()
    {
        CreateMap<CustomFieldValue, CustomFieldValueDto>()
            .ForCtorParam("FieldName", o => o.MapFrom(s => s.Definition != null ? s.Definition.FieldName : string.Empty));
    }
}
