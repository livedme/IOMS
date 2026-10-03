using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record RfqRequestDto(Guid Id, string RfqNumber, RfqStatus Status, DateTime? RequiredDate,
    string? Notes, List<RfqItemDto> Items, List<RfqSupplierResponseDto> SupplierResponses);

public record RfqItemDto(Guid Id, Guid ProductId, string ProductName, int Quantity,
    decimal? TargetUnitPrice);

public record RfqSupplierResponseDto(Guid Id, Guid SupplierId, string SupplierName,
    decimal QuotedPrice, int LeadTimeDays, DateTime? ValidUntil, bool IsSelected);

// RFQ Creation
public record CreateRfqRequestDto(DateTime? RequiredDate, string? Notes, List<CreateRfqItemDto> Items);
public record CreateRfqItemDto(Guid ProductId, int Quantity, decimal? TargetUnitPrice);
public record CreateRfqSupplierResponseDto(Guid RfqRequestId, Guid SupplierId, decimal QuotedPrice,
    int LeadTimeDays, DateTime? ValidUntil, string? Notes);
