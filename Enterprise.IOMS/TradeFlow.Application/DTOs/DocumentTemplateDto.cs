namespace TradeFlow.Application.DTOs;

public record DocumentTemplateDto(Guid Id, string Name, string Type, string HtmlContent, bool IsDefault);
public record CreateDocumentTemplateDto(string Name, string Type, string HtmlContent, bool IsDefault);
