using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record DeliveryNoteDto(Guid Id, string DeliveryNoteNumber, Guid SalesOrderId,
    string SalesOrderNumber, DateTime Date, string? ShippedBy, string? TrackingNumber);

public record CreateDeliveryNoteDto(Guid SalesOrderId, Guid? WarehouseId,
    DateTime? ShippedDate, string? Notes);

public record ShipmentDto(Guid Id, Guid? DeliveryNoteId, string? Carrier, string? TrackingNumber,
    DateTime? ShippedDate, DateTime? DeliveredDate, ShipmentStatus Status);

public record CreateShipmentDto(Guid DeliveryNoteId, string Carrier, string? TrackingNumber,
    DateTime? ShippedDate);
