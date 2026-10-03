namespace TradeFlow.Domain.Entities;

public class Supplier : BaseEntity
{
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierEmail { get; set; }
    public string? SupplierPhone { get; set; }
    public string ContactPersonName { get; set; } = string.Empty;
    public string? ContactPersonEmail { get; set; }
    public string? ContactPersonPhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Zila { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? PaymentTerms { get; set; }
    public Guid? DefaultCurrencyId { get; set; }
    public Currency? DefaultCurrency { get; set; }
    public int LeadTimeDays { get; set; } = 1;
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<RfqSupplierResponse> RfqResponses { get; set; } = new List<RfqSupplierResponse>();
}
