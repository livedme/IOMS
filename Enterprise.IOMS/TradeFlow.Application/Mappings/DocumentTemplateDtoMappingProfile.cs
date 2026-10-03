using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class DocumentTemplateDtoMappingProfile : Profile
{
    public DocumentTemplateDtoMappingProfile()
    {
        CreateMap<DocumentTemplate, DocumentTemplateDto>();
    }
}
