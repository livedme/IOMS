using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IStocktakeService
{
    Task<PagedResult<StocktakeDto>> GetStocktakes(StocktakeStatus? status, int page, int pageSize);
    Task<PagedResultNew<StocktakeDto>> GetStocktakesPagedAsync(StocktakePagedRequest request);
    Task<StocktakeDto> GetStocktakeById(Guid id);
    Task<StocktakeVarianceDto> GetVarianceReport(Guid stocktakeId);
    Task<Guid> CreateStocktake(CreateStocktakeDto dto);
    Task RecordCount(RecordStocktakeCountDto dto);
    Task ApproveStocktake(Guid stocktakeId);
    Task CancelStocktake(Guid stocktakeId);
}
