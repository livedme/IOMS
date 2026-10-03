using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IShippingService
{
    Task<Guid> CreateDeliveryNote(CreateDeliveryNoteDto dto);
    Task<Guid> CreateShipment(CreateShipmentDto dto);
    Task UpdateShipmentStatus(Guid id, ShipmentStatus status);
    Task<PagedResult<ShipmentDto>> GetShipments(string? search, int page, int pageSize);
    Task<PagedResult<DeliveryNoteDto>> GetDeliveryNotesPagedAsync(string? search, int page, int pageSize);
}
