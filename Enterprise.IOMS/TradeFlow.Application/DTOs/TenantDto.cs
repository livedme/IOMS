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
        DateTime? OnboardingCompletedAt);

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
        public string? CountryCode { get; init; }
        public string? Subdomain { get; init; }
        public string DefaultCurrencyCode { get; init; } = "BDT";
        public string DefaultTimeZone { get; init; } = "Asia/Dhaka";
        public string DefaultCulture { get; init; } = "en-US";
    }
}