using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface ISalesReturnService
{
    Task<PagedResult<SalesReturnDto>> GetSalesReturns(string? search, SalesReturnStatus? status, int page, int pageSize);
    Task<PagedResultNew<SalesReturnDto>> GetSalesReturnsAsync(SalesReturnPagedRequest request);
    Task<SalesReturnDto> GetSalesReturnById(Guid id);
    Task<Guid> CreateSalesReturn(SalesReturnDto dto);
    Task ApproveSalesReturn(Guid id);
    Task CompleteSalesReturn(Guid id);
}
