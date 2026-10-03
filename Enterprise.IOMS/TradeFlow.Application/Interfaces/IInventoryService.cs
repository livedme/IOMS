using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IInventoryService
{
    Task<int> GetStockLevel(Guid productId, Guid warehouseId);
    Task AdjustStock(Guid productId, Guid warehouseId, int quantityChange, string reason);
    Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity);
    Task<List<LowStockAlertDto>> GetLowStockAlerts();
    Task<PagedResult<InventoryDto>> GetInventoryByWarehouse(Guid warehouseId, int page, int pageSize);
    Task<PagedResultNew<StockMovementDto>> GetStockMovementsPagedAsync(StockMovementPagedRequest request);
    Task<PagedResultNew<StockLevelDto>> GetStockLevelsPagedAsync(StockLevelPagedRequest request);

    // Extended inventory tracking
    Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouse(Guid warehouseId, int page, int pageSize);
}
