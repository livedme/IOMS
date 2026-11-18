namespace TradeFlow.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }

    public string CompanyCode { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public string? LogoUrl { get; set; }

    public string? Industry { get; set; }

    public string? Website { get; set; }

    public string? PrimaryContactEmail { get; set; }

    public string? PrimaryContactPhone { get; set; }

    public string? BillingEmail { get; set; }

    public string DefaultCurrencyCode { get; set; } = "BDT";

    public string DefaultTimeZone { get; set; } = "Asia/Dhaka";

    public string DefaultCulture { get; set; } = "en-US";

    public string? CountryCode { get; set; }
    public string? ApplicationDomain { get; set; }    
    public Guid? LicenseId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? SubscriptionNumber { get; set; }
    public string? SubscriptionName { get; set; }
    public string? LicensePlan { get; set; }
    public string? LicenseKey { get; set; }
    public string? LicenseBillingCycle { get; set; }    
    public string? LicenseStatus { get; set; }
    public Guid? LicenseProjectId { get; set; }
    public string? LicenseProjectName { get; set; }
    public DateTime? LicenseActivatedAt { get; set; }
    public DateTime? LicenseValidFrom { get; set; }
    public DateTime? LicenseValidUntil { get; set; }
    public DateTime? LicenseCheckedAtUtc { get; set; }
    public decimal LicenseAmount { get; set; }
    public int LicenseGracePeriod { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? StatusReason { get; set; }

    public TenantOnboardingStatus OnboardingStatus { get; set; } = TenantOnboardingStatus.Registered;

    public DateTime? OnboardingCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public TenantStatus TenantStatus { get; set; } = TenantStatus.Pending;

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
    public ICollection<TenantSetting> TenantSettings { get; set; } = new List<TenantSetting>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

public enum TenantStatus
{
    Base = 0,              // base tenant for control everything default status
    Active = 1,              // Free trial period
    Pending = 2,            // Created but awaiting payment or activation
    InActive = 3,             // Currently active
    Deleted = 4,        // Expired but still accessible for a limited time
    Cancelled = 5,          // Cancelled by customer or admin
    Trial = 6,           // Trial period
    PastDue = 7,         // Payment failed but still accessible
    Suspended = 8            // Suspended by platform admin
}

public enum TenantOnboardingStatus
{
    Registered = 0,
    CompanyDetailsCompleted = 1,
    Completed = 2
}
