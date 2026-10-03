using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CreateSalesOrder(CreateSalesOrderDto dto);
    Task UpdateSalesOrder(Guid id, CreateSalesOrderDto dto);
    Task DeleteSalesOrder(Guid id);
    Task ApproveOrder(Guid orderId);
    Task CancelOrder(Guid orderId, string reason);
    Task UpdateOrderStatus(Guid orderId, OrderStatus newStatus);
    Task<SalesOrderDto?> GetSalesOrderById(Guid id);
    Task<PagedResult<SalesOrderDto>> GetSalesOrders(string? search, List<OrderStatus?>? status, int page, int pageSize);
    Task<PagedResultNew<SalesOrderDto>> GetSalesOrdersAsync(SalesPagedRequest request);
}
