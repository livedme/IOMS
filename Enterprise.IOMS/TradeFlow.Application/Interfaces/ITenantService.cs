using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ITenantService
{
    /// <summary>True when a tenant has already claimed the company code.</summary>
    Task<bool> CompanyCodeExistsAsync(string companyCode, CancellationToken cancellationToken = default);

    /// <summary>Generates a unique company code from the company name.</summary>
    Task<string> GenerateCompanyCodeAsync(string companyName, CancellationToken cancellationToken = default);

    /// <summary>True when a tenant has already claimed the subdomain.</summary>
    Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a tenant profile and records it in the current scope so the audit interceptor can
    /// stamp the tenant and the administrator user created immediately afterwards.
    /// </summary>
    Task<TenantDto> CreateTenantAsync(CreateTenantDto dto, CancellationToken cancellationToken = default);

    /// <summary>Removes a tenant profile, used to roll back a failed registration.</summary>
    Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
