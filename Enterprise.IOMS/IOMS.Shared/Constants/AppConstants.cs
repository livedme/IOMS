namespace IOMS.Shared.Constants;

public static class AppConstants
{
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxExportRows = 10000;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Sales = "Sales";
    public const string Purchasing = "Purchasing";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Manager, Sales, Purchasing, WarehouseStaff, Viewer];
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
