namespace TradeFlow.Application.DTOs
{
    public record TenantDto(
        Guid Id,
        string CompanyCode,
        string CompanyName,
        string TenantStatus,
        DateTime CreatedAt);

    public record TenantProfileDto(
        Guid Id,
        string CompanyCode,
        string CompanyName,
        string? LegalName,
        string? LogoUrl,
        string? Industry,
        string? Website,
        string? PrimaryContactEmail,
        string? PrimaryContactPhone,
        string? BillingEmail,
        string? Address,
        string? City,
        string? State,
        string? Country,
        string? CountryCode,
        string DefaultCurrencyCode,
        string DefaultTimeZone,
        string DefaultCulture,        
        string? ApplicationDomain,
        string TenantStatus,
        string? StatusReason,
        string OnboardingStatus,
        DateTime? TrialEndsAt,
        DateTime? ActivatedAt,
        DateTime? SuspendedAt,
        DateTime? CancelledAt,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? OnboardingCompletedAt,
        Guid? LicenseId,
        Guid? SubscriptionId,
        string? SubscriptionNumber,
        string? SubscriptionName,
        string? LicensePlan,
        string? LicenseBillingCycle,
        string? LicenseStatus,
        Guid? LicenseProjectId,
        string? LicenseProjectName,
        string? LicenseKey,
        DateTime? LicenseActivatedAt,
        DateTime? LicenseValidFrom,
        DateTime? LicenseValidUntil,
        DateTime? LicenseCheckedAtUtc,
        decimal LicenseAmount,
        int LicenseGracePeriod);

    public record UpdateTenantProfileDto
    {
        public required string CompanyName { get; set; }
        public string? LegalName { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? CountryCode { get; set; }
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactPhone { get; set; }
        public string? BillingEmail { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string DefaultCurrencyCode { get; set; } = "BDT";
        public string DefaultTimeZone { get; set; } = "Asia/Dhaka";
        public string DefaultCulture { get; set; } = "en-US";
    }

    public record CreateTenantDto
    {
        public required string CompanyName { get; init; }
        public required string CompanyCode { get; init; }
        public string? LegalName { get; init; }
        public string? Industry { get; init; }
        public string? Website { get; init; }
        public string? PrimaryContactEmail { get; init; }
        public string? PrimaryContactPhone { get; init; }
        public required string CreatedBy { get; init; }
        public string? BillingEmail { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? Country { get; init; }
        public string? CountryCode { get; init; }
        public string? Subdomain { get; init; }
        public string DefaultCurrencyCode { get; init; } = "BDT";
        public string DefaultTimeZone { get; init; } = "Asia/Dhaka";
        public string DefaultCulture { get; init; } = "en-US";
    }
}