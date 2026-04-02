using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

public class TaxRate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public TaxType TaxType { get; set; }
    public Guid? TaxJurisdictionId { get; set; }
    public TaxJurisdiction? TaxJurisdiction { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCompound { get; set; }
}

public class TaxJurisdiction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? Description { get; set; }
    public ICollection<TaxRate> TaxRates { get; set; } = new List<TaxRate>();
}

public class TaxExemption : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string ExemptionCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class PriceList : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}

public class PriceListItem : BaseEntity
{
    public Guid PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int? MinQuantity { get; set; }
}

public class Discount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public decimal? ApprovalThreshold { get; set; }
}

public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ExchangeRate> FromExchangeRates { get; set; } = new List<ExchangeRate>();
    public ICollection<ExchangeRate> ToExchangeRates { get; set; } = new List<ExchangeRate>();
}

public class ExchangeRate : BaseEntity
{
    public Guid FromCurrencyId { get; set; }
    public Currency FromCurrency { get; set; } = null!;
    public Guid ToCurrencyId { get; set; }
    public Currency ToCurrency { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
}
