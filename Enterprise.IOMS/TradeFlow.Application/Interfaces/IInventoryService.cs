using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IInventoryService
{
    Task<int> GetStockLevel(Guid productId, Guid warehouseId);
    Task AdjustStock(Guid productId, Guid warehouseId, int quantityChange, string reason);
    Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity, DateTime? movementDate = null, string? notes = null);
    Task<string> CreateStockTransferAsync(StockTransferCreateDto dto);
    Task<string> UpdateStockTransferAsync(Guid id, StockTransferCreateDto dto);
    Task CancelStockTransferAsync(Guid id);
    Task<List<LowStockAlertDto>> GetLowStockAlerts();
    Task<PagedResult<InventoryDto>> GetInventoryByWarehouse(Guid warehouseId, int page, int pageSize);
    Task<PagedResultNew<StockMovementDto>> GetStockMovementsPagedAsync(StockMovementPagedRequest request);
    Task<PagedResultNew<StockTransferListDto>> GetStockTransfersPagedAsync(StockTransferPagedRequest request);
    Task<StockTransferListDto> GetStockTransferByIdAsync(Guid id);
    Task<PagedResultNew<StockLevelDto>> GetStockLevelsPagedAsync(StockLevelPagedRequest request);

    // Extended inventory tracking
    Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouse(Guid warehouseId, int page, int pageSize);
    Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouses(IEnumerable<Guid> warehouseIds, int page, int pageSize);
}
