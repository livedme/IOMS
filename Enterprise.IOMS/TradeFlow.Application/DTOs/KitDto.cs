namespace TradeFlow.Application.DTOs;

public record KitDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    bool IsActive, List<KitComponentDto> Components);

public record KitComponentDto(Guid Id, Guid ComponentProductId, string ComponentProductName,
    string ComponentProductSKU, int Quantity);

// Kit Creation
public record CreateKitDto(Guid ProductId, List<CreateKitComponentDto> Components);
public record CreateKitComponentDto(Guid ComponentProductId, int Quantity);
public record KitAssemblyDto(Guid KitId, Guid WarehouseId, int Quantity);
