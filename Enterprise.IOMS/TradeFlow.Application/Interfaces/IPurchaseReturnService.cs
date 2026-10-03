using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IPurchaseReturnService
{
    Task<PagedResult<PurchaseReturnDto>> GetPurchaseReturns(string? search, PurchaseReturnStatus? status, int page, int pageSize);
    Task<PagedResultNew<PurchaseReturnDto>> GetPurchaseReturnsAsync(PurchaseReturnPagedRequest request);
    Task<PurchaseReturnDto> GetPurchaseReturnById(Guid id);
    Task<Guid> CreatePurchaseReturn(PurchaseReturnDto dto);
    Task ApprovePurchaseReturn(Guid id);
    Task CompletePurchaseReturn(Guid id);
}
