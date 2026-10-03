using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ISearchService
{
    Task<List<GlobalSearchResultDto>> Search(string query, int maxResults = 20);
}
