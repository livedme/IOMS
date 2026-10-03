using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IWarehousesService
{
    Task<List<WarehouseDto>> GetAllWarehousesAsync();

    /// <summary>
    /// Active warehouses, for the stocktake dialog which used to load them straight off the context
    /// and hand the tracked entities to a MudBlazor select.
    /// </summary>
    Task<List<WarehouseDto>> GetActiveWarehousesAsync();

    Task<WarehouseDto> GetWarehouseByIdAsync(Guid id);
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, int page, int pageSize);
    Task<PagedResultNew<WarehouseDto>> GetWarehousesPagedAsync(WarehousePagedRequest request);    
    Task<Guid> CreateWarehouseAsync(CreateWarehouseDto dto);
    Task UpdateWarehouseAsync(Guid id, CreateWarehouseDto dto);
    Task DeleteWarehouseAsync(Guid id);
}
