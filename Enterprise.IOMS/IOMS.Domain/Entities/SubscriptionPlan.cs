using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IOMS.Domain.Entities
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal MonthlyPrice { get; set; }

        public decimal YearlyPrice { get; set; }

        public SubscriptionPlanType PlanType { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxUsers { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxBranches { get; set; }

        [Range(0, int.MaxValue)]
        public int MinUsers { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxTrucks { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxDrivers { get; set; }

        [Range(0, int.MaxValue)]
        public int MinDrivers { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxStorageGB { get; set; }

        public bool AiEnabled { get; set; }

        public bool GpsEnabled { get; set; }

        public bool IoTEnabled { get; set; }

        public bool DriverAppEnabled { get; set; }

        public bool EPODEnabled { get; set; }

        public bool FuelModuleEnabled { get; set; }

        public bool MaintenanceModuleEnabled { get; set; }

        public bool AccountingModuleEnabled { get; set; }

        public bool InventoryModuleEnabled { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsPopular { get; set; } = false;

        public ICollection<TenantSubscription> Tenants { get; set; } = new List<TenantSubscription>();
    }
    public class TenantSubscription
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; } = default!;
        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = default!;

        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public SubscriptionStatus Status { get; set; }

        public bool AutoRenew { get; set; }

        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
    public enum SubscriptionPlanType
    {
        Basic = 1,
        Professional = 2,
        Enterprise = 3
    }
    public enum SubscriptionStatus
    {
        Trial = 1,              // Free trial period

        Pending = 2,            // Created but awaiting payment or activation

        Active = 3,             // Currently active

        GracePeriod = 4,        // Expired but still accessible for a limited time

        Suspended = 5,          // Suspended due to billing or admin action

        PastDue = 6,            // Payment overdue

        Expired = 7,            // Subscription ended

        Cancelled = 8,          // Cancelled by customer or admin

        RenewalPending = 9,     // Renewal initiated, awaiting payment

        Upgraded = 10,          // Replaced by a higher-tier plan

        Downgraded = 11,        // Replaced by a lower-tier plan

        Archived = 12           // Historical record retained for auditing
    }
    public enum BillingCycle
    {
        Monthly = 1,
        Yearly = 2
    }
}
