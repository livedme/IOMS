using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IPurchaseService
{
    Task<Guid> CreatePurchaseOrder(CreatePurchaseOrderDto dto);
    Task UpdatePurchaseOrder(Guid id, CreatePurchaseOrderDto dto);
    Task CancelPurchaseOrder(Guid id);
    Task DeletePurchaseOrder(Guid id);
    Task ApprovePurchaseOrder(Guid poId);
    Task ReceiveGoods(Guid poId, List<GoodsReceivedLineDto> lines);
    Task<PurchaseOrderDto?> GetPurchaseOrderById(Guid id);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrders(string? search, PurchaseOrderStatus? status, int page, int pageSize);
    Task<PagedResultNew<PurchaseOrderDto>> GetPurchaseOrdersAsync(PurchaseOrderPagedRequest request);

    /// <summary>
    /// Newest purchase orders, number and date only. The landed-cost screen and the purchase-return
    /// dialog both loaded the full tracked entity — with its items — to fill a dropdown.
    /// </summary>
    Task<List<PurchaseOrderOptionDto>> GetRecentPurchaseOrders(int count);
}
