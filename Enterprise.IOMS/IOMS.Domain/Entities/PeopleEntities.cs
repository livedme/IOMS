namespace IOMS.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public Guid? TaxJurisdictionId { get; set; }
    public TaxJurisdiction? TaxJurisdiction { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public PriceList? DefaultPriceList { get; set; }
    public Guid? DefaultCurrencyId { get; set; }
    public Currency? DefaultCurrency { get; set; }
    public bool IsTaxExempt { get; set; }
    public string? TaxExemptionCode { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<SalesQuote> SalesQuotes { get; set; } = new List<SalesQuote>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? PaymentTerms { get; set; }
    public Guid? DefaultCurrencyId { get; set; }
    public Currency? DefaultCurrency { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<RfqSupplierResponse> RfqResponses { get; set; } = new List<RfqSupplierResponse>();
}
