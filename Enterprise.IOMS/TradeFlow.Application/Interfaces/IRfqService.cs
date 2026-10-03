using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IRfqService
{
    Task<PagedResult<RfqRequestDto>> GetRfqRequests(string? search, RfqStatus? status, int page, int pageSize);
    Task<RfqRequestDto> GetRfqRequestById(Guid id);
    Task<Guid> CreateRfqRequest(CreateRfqRequestDto dto);
    Task AddSupplierResponse(CreateRfqSupplierResponseDto dto);
    Task AwardRfq(Guid rfqId, Guid supplierResponseId);
    Task<Guid> ConvertRfqToPurchaseOrder(Guid rfqId, Guid supplierResponseId);
}
