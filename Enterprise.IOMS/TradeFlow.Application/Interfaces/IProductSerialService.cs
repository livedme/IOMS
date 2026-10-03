using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

public interface IProductSerialService
{
    Task<PagedResult<ProductSerialDto>> GetSerials(Guid? productId, ProductSerialStatus? status, int page, int pageSize);
    Task<ProductSerialDto?> GetSerialById(Guid id);
    Task<List<ProductSerialDto>> GetAvailableSerialsByProduct(Guid productId);
    Task<Guid> CreateSerial(CreateProductSerialDto dto);
    Task<List<Guid>> CreateSerials(List<CreateProductSerialDto> dtos);
    Task UpdateStatus(Guid id, ProductSerialStatus status);
    Task DeleteSerial(Guid id);
}
