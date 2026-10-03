using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record PriceListDto(Guid Id, string Name, Guid? CurrencyId, string? CurrencyCode,
    bool IsDefault, DateTime? EffectiveFrom, DateTime? EffectiveTo, bool IsActive,
    List<PriceListItemDto> Items);

public record PriceListItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    decimal UnitPrice, int? MinQuantity);

public record CreatePriceListDto(string Name, string? Description = null,
    DateTime? EffectiveFrom = null, DateTime? EffectiveTo = null,
    bool IsActive = true, Guid? CurrencyId = null, bool IsDefault = false,
    List<CreatePriceListItemDto>? Items = null);

public record CreatePriceListItemDto(Guid ProductId, decimal UnitPrice, int? MinQuantity);

public record DiscountDto(Guid Id, string Name, DiscountType Type, decimal Value,
    int? MinQuantity, int? MaxQuantity, DateTime? StartDate, DateTime? EndDate,
    Guid? ProductId, Guid? CategoryId, Guid? CustomerId, bool IsActive);

public record CreateDiscountDto(string Name, DiscountType DiscountType, decimal Value,
    int? MinQuantity = null, DateTime? StartDate = null, DateTime? EndDate = null,
    bool IsActive = true, Guid? ProductId = null, Guid? CustomerId = null);

public record ResolvedPriceDto(decimal UnitPrice, string PriceListName, decimal DiscountAmount,
    decimal FinalPrice);
