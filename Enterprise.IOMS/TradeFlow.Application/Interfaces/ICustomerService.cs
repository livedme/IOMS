using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, int page = 1, int pageSize = 100);
    Task<PagedResultNew<CustomerDto>> GetCustomersAsync(CustomerPagedRequest request);
    Task<List<string>> GetCustomerCitiesAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);

    /// <summary>
    /// Active customers, for pickers. Was hand-written into seven components against the raw
    /// <c>DbContext</c>, each with a different projection.
    /// </summary>
    Task<List<CustomerDto>> GetActiveCustomersAsync();

    Task<Guid> CreateCustomerAsync(CustomerDto dto);
    Task UpdateCustomerAsync(Guid id, CustomerDto dto);
    Task DeleteCustomerAsync(Guid id);
}
