using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record StocktakeDto(Guid Id, Guid WarehouseId, string WarehouseName, DateTime StartDate,
    DateTime? EndDate, StocktakeStatus Status, int TotalItems, int CountedItems);

public record StocktakeItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int SystemQuantity, int? CountedQuantity, int Variance);

public record StocktakeVarianceDto(Guid StocktakeId, string WarehouseName,
    List<StocktakeItemDto> Items, int TotalVariance, decimal ValueVariance);

// Stocktake Creation
public record CreateStocktakeDto(Guid WarehouseId, string? Notes);
public record RecordStocktakeCountDto(Guid StocktakeItemId, int CountedQuantity, string? Notes);
