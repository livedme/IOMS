using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResultDto> Search(string query, int maxResults = 20);
}
