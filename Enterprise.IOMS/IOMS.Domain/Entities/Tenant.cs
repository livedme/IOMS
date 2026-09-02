using System;
using System.Collections.Generic;

namespace IOMS.Domain.Entities
{

    public class Tenant
    {
        public Guid Id { get; set; }

        public string CompanyCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public TenantStatus TenantStatus { get; set; } = TenantStatus.Pending;

        public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
        public ICollection<TenantSetting> TenantSettings { get; set; } = new List<TenantSetting>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }

    public class TenantSetting : BaseEntity
    {
        /// <summary>
        /// Setting key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Setting value (can be JSON)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Description of this setting
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this is a system-level setting (read-only)
        /// </summary>
        public bool IsSystem { get; set; } = false;

    }
    public enum TenantStatus
    {
        Base = 0,              // base tenant for control everything default status
        Active = 1,              // Free trial period
        Pending = 2,            // Created but awaiting payment or activation
        InActive = 3,             // Currently active
        Deleted = 4,        // Expired but still accessible for a limited time
        Cancelled = 5          // Cancelled by customer or admin

    }
}
