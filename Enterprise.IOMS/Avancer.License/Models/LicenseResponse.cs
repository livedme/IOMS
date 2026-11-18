namespace Avancer.License.Models
{
    /// <summary>
    /// Response returned by <c>GET /api/License/{domainName}/{licenseKey}</c>.
    /// Matches the <c>ActivationDetails</c> contract exposed by the Avancer license server.
    /// </summary>
    public sealed class LicenseResponse
    {
        public Guid ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? ApplicationDomain { get; set; }
        public Guid LicenseId { get; set; }
        public BillingCycle? BillingCycle { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidUntil { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string? SubscriptionNumber { get; set; }
        public string? SubscriptionName { get; set; }
        public SubscriptionType Type { get; set; }
        public RecordStatus Status { get; set; }
        public decimal Amount { get; set; }
        public int GracePeriod { get; set; }
    }
    public enum RecordStatus
    {
        Pending = 1,
        Active = 2,
        InActive = 3,
        Expired = 4,
        Suspended = 5,
        Revoked = 6,
        Cancelled = 7
    }
    public enum BillingCycle
    {
        Monthly = 1,
        Quarterly = 2,
        Yearly = 3
    }
    public enum SubscriptionType
    {
        Basic = 0,
        Advanced = 1,
        Enterprise = 2,
        Custom = 3
    }
}
