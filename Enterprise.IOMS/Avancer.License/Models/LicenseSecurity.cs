namespace Avancer.License
{
    public class LicenseSecurity
    {
        public bool IsLicenseValid { get; set; }
        public Subcription Subcription { get; set; }
    }

    public class  Subcription
    {
        public Guid Id { get; set; }
        public string Plan { get; set; }
        public DateTime ActiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public SubscriptionStatus Status { get; set; }
    }
    public enum SubscriptionStatus
    {
        Trial = 1,
        Active = 2,
        PastDue = 3,
        Suspended = 4,
        Cancelled = 5,
        Expired = 6
    }
}
