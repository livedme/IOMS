using Avancer.License.Models;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Constants;
using LicenseSubscriptionStatus = Avancer.License.Models.SubscriptionStatus;

namespace TradeFlow.Web.Services;

public sealed class LicenseTenantSynchronizer
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LicenseTenantSynchronizer> _logger;

    public LicenseTenantSynchronizer(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider,
        ILogger<LicenseTenantSynchronizer> logger)
    {
        _dbFactory = dbFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task SynchronizeAsync(
        LicenseSecurity result,
        string domain,
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        var response = result.Response;
        if (response is null)
            return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            var tenantId = await ResolveTenantIdAsync(db, response, domain, cancellationToken);
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant is null)
            {
                _logger.LogWarning(
                    "License response could not be persisted because tenant {TenantId} was not found.",
                    tenantId);
                return;
            }

            Apply(tenant, result, response, licenseKey);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to persist license response for domain {Domain}. The license gate still functions.",
                domain);
        }
    }

    public async Task PersistLicenseKeyAsync(
        string licenseKey,
        string domain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            var tenantId = await ResolveTenantIdAsync(db, null, domain, cancellationToken);
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant is null)
            {
                _logger.LogWarning(
                    "License key could not be persisted because tenant {TenantId} was not found.",
                    tenantId);
                return;
            }

            tenant.LicenseKey = licenseKey.Trim();
            tenant.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Failed to persist the submitted license key for domain {Domain}.",
                domain);
        }
    }

    private async Task<Guid> ResolveTenantIdAsync(
        ApplicationDbContext db,
        LicenseResponse? response,
        string domain,
        CancellationToken cancellationToken)
    {
        var authenticatedTenant = _tenantProvider.TryGetTenantId();
        if (authenticatedTenant is { } tenantId && tenantId != Guid.Empty)
            return tenantId;

        var candidates = new[] { domain, response?.ApplicationDomain }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var candidate in candidates)
        {
            var subdomain = candidate.Split('.')[0];

            var match = await db.Tenants.FirstOrDefaultAsync(
                tenant => (tenant.ApplicationDomain != null && tenant.ApplicationDomain == candidate), cancellationToken);

            if (match is not null)
                return match.Id;
        }

        return Guid.Parse(AppConstants.DefaultTenantId);
    }

    private static void Apply(
        Tenant tenant,
        LicenseSecurity result,
        LicenseResponse response,
        string licenseKey)
    {
        var status = result.Subscription?.Status ?? ParseStatus(response.Status);

        tenant.LicenseKey = licenseKey;
        tenant.LicenseId = response.LicenseId;
        tenant.SubscriptionId = response.SubscriptionId;
        tenant.SubscriptionNumber = response.SubscriptionNumber;
        tenant.SubscriptionName = response.SubscriptionName;
        tenant.LicensePlan = response.Type.ToString();
        tenant.LicenseBillingCycle = response.BillingCycle?.ToString();
        tenant.ApplicationDomain = response.ApplicationDomain;
        tenant.LicenseStatus = status.ToString();
        tenant.LicenseProjectId = response.ProjectId == Guid.Empty ? null : response.ProjectId;
        tenant.LicenseProjectName = response.ProjectName;
        tenant.LicenseActivatedAt = NormalizeUtc(response.ActivatedAtUtc);
        tenant.LicenseValidFrom = response.ValidFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        tenant.LicenseValidUntil = response.ValidUntil?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        tenant.LicenseCheckedAtUtc = result.CheckedAtUtc;
        tenant.LicenseAmount = response.Amount;
        tenant.LicenseGracePeriod = response.GracePeriod;

        tenant.TenantStatus = MapTenantStatus(status);
        tenant.ActivatedAt = tenant.LicenseActivatedAt;
        tenant.StatusReason = result.Message;
        tenant.UpdatedAt = DateTime.UtcNow;

        if (status == LicenseSubscriptionStatus.Trial)
            tenant.TrialEndsAt = tenant.LicenseValidUntil;

        if (status == LicenseSubscriptionStatus.Suspended)
            tenant.SuspendedAt ??= DateTime.UtcNow;

        if (status is LicenseSubscriptionStatus.Cancelled or LicenseSubscriptionStatus.Revoked)
            tenant.CancelledAt ??= DateTime.UtcNow;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue || value.Value == default)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static LicenseSubscriptionStatus ParseStatus(RecordStatus status) => status switch
    {
        RecordStatus.Pending => LicenseSubscriptionStatus.Pending,
        RecordStatus.Active => LicenseSubscriptionStatus.Active,
        RecordStatus.InActive => LicenseSubscriptionStatus.Inactive,
        RecordStatus.Expired => LicenseSubscriptionStatus.Expired,
        RecordStatus.Suspended => LicenseSubscriptionStatus.Suspended,
        RecordStatus.Revoked => LicenseSubscriptionStatus.Revoked,
        RecordStatus.Cancelled => LicenseSubscriptionStatus.Cancelled,
        _ => LicenseSubscriptionStatus.Expired
    };

    private static TenantStatus MapTenantStatus(LicenseSubscriptionStatus status) => status switch
    {
        LicenseSubscriptionStatus.Trial => TenantStatus.Trial,
        LicenseSubscriptionStatus.Active => TenantStatus.Active,
        LicenseSubscriptionStatus.Pending => TenantStatus.Pending,
        LicenseSubscriptionStatus.Inactive => TenantStatus.InActive,
        LicenseSubscriptionStatus.Expired => TenantStatus.InActive,
        LicenseSubscriptionStatus.Suspended => TenantStatus.Suspended,
        LicenseSubscriptionStatus.Revoked => TenantStatus.Cancelled,
        LicenseSubscriptionStatus.Cancelled => TenantStatus.Cancelled,
        LicenseSubscriptionStatus.PastDue => TenantStatus.PastDue,
        _ => TenantStatus.Pending
    };
}
