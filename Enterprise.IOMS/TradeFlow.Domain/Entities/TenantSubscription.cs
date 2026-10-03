namespace TradeFlow.Domain.Entities;

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
