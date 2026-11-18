namespace Avancer.License.Models
{
    public sealed class LicenseSecurity
    {
        public bool IsLicenseValid { get; set; }

        public bool IsOffline { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

        public Subscription? Subscription { get; set; }

        public LicenseResponse? Response { get; set; }
    }

    public sealed class Subscription
    {
        public Guid Id { get; set; }

        public Guid LicenseId { get; set; }

        public Guid? SubscriptionId { get; set; }

        public string? SubscriptionNumber { get; set; }

        public string? SubscriptionName { get; set; }

        public Guid ProjectId { get; set; }

        public string? ProjectName { get; set; }

        public string? DomainName { get; set; }

        public string? Plan { get; set; }

        public BillingCycle? BillingCycle { get; set; }

        public SubscriptionType Type { get; set; }

        public RecordStatus RecordStatus { get; set; }

        public decimal Amount { get; set; }

        public int GracePeriod { get; set; }

        public DateTime ActiveDate { get; set; }

        public DateTime ValidUntil { get; set; } = DateTime.MaxValue;

        public DateTime ExpiryDate { get; set; } = DateTime.MaxValue;

        public SubscriptionStatus Status { get; set; }
    }

    public enum SubscriptionStatus
    {
        Trial = 1,
        Active = 2,
        Expired = 3,
        Suspended = 4,
        Revoked = 5,
        Pending = 6,
        PastDue = 7,
        Cancelled = 8,
        Inactive = 9
    }
}
