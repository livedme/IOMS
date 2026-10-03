using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IDocumentTemplateService
{
    Task<List<DocumentTemplateDto>> GetTemplates();
    Task<Guid> CreateTemplate(CreateDocumentTemplateDto dto);
    Task UpdateTemplate(Guid id, CreateDocumentTemplateDto dto);
    Task DeleteTemplate(Guid id);
}
