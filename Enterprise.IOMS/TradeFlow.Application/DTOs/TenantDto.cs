namespace TradeFlow.Application.DTOs
{
    public record TenantDto(
        Guid Id,
        string CompanyCode,
        string CompanyName,
        string TenantStatus,
        DateTime CreatedAt);

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