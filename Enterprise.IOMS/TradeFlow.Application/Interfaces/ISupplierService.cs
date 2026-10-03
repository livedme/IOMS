using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(string? search = "", int page = 1, int pageSize = 100);
    Task<PagedResultNew<SupplierDto>> GetSuppliersAsync(SupplierPagedRequest request);
    Task<List<string>> GetSupplierCitiesAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id);

    /// <summary>Active suppliers, for pickers. See <see cref="ICustomerService.GetActiveCustomersAsync"/>.</summary>
    Task<List<SupplierDto>> GetActiveSuppliersAsync();

    Task<Guid> CreateSupplierAsync(SupplierDto dto);
    Task UpdateSupplierAsync(Guid id, SupplierDto dto);
    Task DeleteSupplierAsync(Guid id);
}
