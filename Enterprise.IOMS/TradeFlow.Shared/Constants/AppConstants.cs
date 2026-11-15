using System.Security.Claims;

namespace TradeFlow.Shared.Constants;

public static class AppConstants
{
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxExportRows = 10000;
}

public static class Roles
{
    /// <summary>Platform-wide administrator with access to every tenant and system feature.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>Platform-wide administrator with access to every tenant and system feature.</summary>
    public const string SuperAdmin = "SuperAdmin";
    public const string Manager = "Manager";
    public const string FinanceManager = "FinanceManager";

    /// <summary>
    /// Administrator of a single tenant/workspace. Created automatically for the first user
    /// who registers a workspace.
    /// </summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>
    /// Legacy platform-administrator alias. Existing platform-level checks use this name; it
    /// resolves to <see cref="SystemAdmin"/> so those checks never grant tenant admins
    /// cross-tenant access.
    /// </summary>    
    public const string SalesManager = "SalesManager";
    public const string SalesStaff = "SalesStaff";
    public const string Staff = "Staff";
    public const string AccountManager = "AccountManager";

    // Legacy roles retained for existing users and seeded data.
    public const string Sales = "Sales";
    public const string Purchasing = "Purchasing";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        SystemAdmin, SuperAdmin, TenantAdmin,
        Manager, FinanceManager, SalesManager, SalesStaff, Staff, AccountManager,
        Sales, Purchasing, WarehouseStaff, Viewer
    ];

    /// <summary>Roles that grant platform-wide access across every tenant.</summary>
    public static readonly string[] PlatformAdministrators = [SystemAdmin, SuperAdmin];

    /// <summary>Roles a platform administrator can assign when creating a user.</summary>
    public static readonly string[] PlatformManagedRoles = [Manager, FinanceManager];

    /// <summary>Roles a tenant administrator can assign to users in their own tenant.</summary>
    public static readonly string[] TenantManagedRoles = [SalesManager, SalesStaff, Staff, AccountManager];

    public static bool IsPlatformAdmin(ClaimsPrincipal? principal)
        => principal?.IsInRole(SystemAdmin) == true || principal?.IsInRole(SuperAdmin) == true;

    public static bool IsTenantAdmin(ClaimsPrincipal? principal)
        => principal?.IsInRole(TenantAdmin) == true;
}

public static class AuthorizationPolicies
{
    /// <summary>System administration: users, roles, audit logs, privacy and archival tools.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Platform administration: roles and other cross-tenant settings.</summary>
    public const string PlatformAdministrator = "PlatformAdministrator";

    /// <summary>Day-to-day operational management: reports, approvals, configuration.</summary>
    public const string Manager = "Manager";
}

public static class AccountCodes
{
    public const string Cash = "1000";
    public const string AccountsReceivable = "1100";
    public const string Inventory = "1200";
    public const string AccountsPayable = "2000";
    public const string OwnersEquity = "3000";
    public const string SalesRevenue = "4000";
    public const string CostOfGoodsSold = "5000";
    public const string OperatingExpenses = "5100";
    public const string InventoryAdjustment = "5200";
    public const string ExchangeGainLoss = "6000";
    public const string SalesReturns = "4100";
}

public static class DocumentTypes
{
    public const string SalesOrder = "SalesOrder";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string SalesQuote = "SalesQuote";
    public const string Invoice = "Invoice";
    public const string DeliveryNote = "DeliveryNote";
    public const string CreditNote = "CreditNote";
    public const string DebitNote = "DebitNote";
    public const string PurchaseReturn = "PurchaseReturn";
}
