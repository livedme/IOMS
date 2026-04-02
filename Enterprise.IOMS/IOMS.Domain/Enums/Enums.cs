namespace IOMS.Domain.Enums;

public enum OrderStatus
{
    Pending, Approved, Packed, Shipped, PartiallyShipped, Delivered, Cancelled
}

public enum PurchaseOrderStatus
{
    Draft, Submitted, Approved, PartiallyReceived, Received, Closed, Cancelled
}

public enum AccountType
{
    Asset, Liability, Equity, Revenue, Expense
}

public enum StockMovementType
{
    In, Out, Transfer, Adjustment, Return, WriteOff, KitAssembly, KitDisassembly
}

public enum PaymentMethod
{
    Cash, BankTransfer, Check, CreditCard, Other
}

public enum QuoteStatus
{
    Draft, Sent, Accepted, Rejected, Expired, Converted
}

public enum TaxType
{
    VAT, GST, SalesTax, ServiceTax, Exempt
}

public enum DiscountType
{
    Percentage, FixedAmount
}

public enum CreditNoteStatus
{
    Draft, Approved, Applied, Cancelled
}

public enum DebitNoteStatus
{
    Draft, Approved, Applied, Cancelled
}

public enum ShipmentStatus
{
    Pending, InTransit, Delivered, Failed, Returned
}

public enum StocktakeStatus
{
    Draft, InProgress, PendingApproval, Approved, Cancelled
}

public enum PurchaseReturnStatus
{
    Draft, Approved, Shipped, Completed, Cancelled
}

public enum RfqStatus
{
    Draft, Sent, Received, Awarded, Closed, Cancelled
}

public enum ApprovalDecision
{
    Pending, Approved, Rejected, Escalated
}

public enum NotificationChannel
{
    InApp, Email, SMS
}

public enum CustomFieldType
{
    Text, Number, Date, Dropdown, Checkbox, MultiSelect
}

public enum DataSubjectRequestType
{
    Access, Erasure
}

public enum DataSubjectRequestStatus
{
    Pending, InProgress, Completed, Cancelled
}

public enum LandedCostAllocMethod
{
    ByValue, ByWeight, ByVolume, ByQuantity
}

public enum InvoiceType
{
    Sales, Purchase
}

public enum InvoiceStatus
{
    Draft, Sent, Paid, PartiallyPaid, Overdue, Cancelled
}

public enum PaymentStatus
{
    Pending, Completed, Failed, Refunded
}

public enum PaymentType
{
    Receipt, Payment
}
