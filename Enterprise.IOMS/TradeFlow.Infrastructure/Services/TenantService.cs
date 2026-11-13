using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure.Data;

namespace TradeFlow.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly TenantContext _tenantContext;

    public TenantService(IDbContextFactory<ApplicationDbContext> dbFactory, TenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<bool> CompanyCodeExistsAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tenants.AnyAsync(t => t.CompanyCode == companyCode, cancellationToken);
    }

    public async Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tenants.AnyAsync(t => t.Subdomain == subdomain, cancellationToken);
    }

    public async Task<string> GenerateCompanyCodeAsync(string companyName, CancellationToken cancellationToken = default)
    {
        var baseCode = ToBaseCode(companyName);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var candidate = baseCode;
        var suffix = 2;
        while (await db.Tenants.AnyAsync(t => t.CompanyCode == candidate, cancellationToken))
        {
            var suffixText = $"-{suffix++}";
            var prefixLength = Math.Max(1, 20 - suffixText.Length);
            var prefix = baseCode.Length > prefixLength
                ? baseCode[..prefixLength].TrimEnd('-')
                : baseCode;

            candidate = prefix + suffixText;
        }

        return candidate;
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var companyCode = dto.CompanyCode.Trim();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            CompanyCode = companyCode,
            CompanyName = dto.CompanyName.Trim(),
            LegalName = TrimOrNull(dto.LegalName),
            Industry = TrimOrNull(dto.Industry),
            Website = TrimOrNull(dto.Website),
            PrimaryContactEmail = TrimOrNull(dto.PrimaryContactEmail),
            PrimaryContactPhone = TrimOrNull(dto.PrimaryContactPhone),
            BillingEmail = TrimOrNull(dto.BillingEmail),
            CountryCode = TrimOrNull(dto.CountryCode)?.ToUpperInvariant(),
            Subdomain = ToSubdomain(dto.Subdomain, companyCode),
            DefaultCurrencyCode = Normalize(dto.DefaultCurrencyCode, "BDT").ToUpperInvariant(),
            DefaultTimeZone = Normalize(dto.DefaultTimeZone, "Asia/Dhaka"),
            DefaultCulture = Normalize(dto.DefaultCulture, "en-US"),
            TenantStatus = TenantStatus.Active,
            ActivatedAt = now,
            OnboardingStatus = TenantOnboardingStatus.Completed,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            CreatedBy = dto.CreatedBy
        };

        // Registration runs before the administrator is signed in, so record the new tenant in
        // the scoped context. The audit interceptor reads it when this tenant is saved and when
        // the administrator account is created on the same scope.
        _tenantContext.Set(tenant.Id, null);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        return new TenantDto(
            tenant.Id,
            tenant.CompanyCode,
            tenant.CompanyName,
            tenant.TenantStatus.ToString(),
            tenant.CreatedAt);
    }

    public async Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _tenantContext.Set(tenantId, null);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Normalize(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string ToBaseCode(string companyName)
    {
        var slug = System.Text.RegularExpressions.Regex
            .Replace(companyName.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-")
            .Trim('-');

        if (slug.Length == 0)
        {
            slug = "tenant";
        }

        return slug.Length > 20 ? slug[..20].TrimEnd('-') : slug;
    }

    private static string ToSubdomain(string? value, string companyCode)
    {
        var source = string.IsNullOrWhiteSpace(value) ? companyCode : value;
        var slug = System.Text.RegularExpressions.Regex
            .Replace(source.Trim().ToLowerInvariant(), "[^a-z0-9-]+", "-")
            .Trim('-');

        return slug.Length > 63 ? slug[..63].Trim('-') : slug;
    }
}
