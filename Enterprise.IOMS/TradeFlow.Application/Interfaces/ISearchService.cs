using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResultDto> Search(string query, IReadOnlyCollection<SearchEntity> entities, int maxResults = 20);
}
