namespace TradeFlow.Application.DTOs;

public record GlobalSearchResultDto(string EntityType, Guid Id, string Title, string SubTitle, string Url);
