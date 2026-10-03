namespace TradeFlow.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? BrandCode { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = "Active";
    public string? OriginCompany { get; set; }
    public string? OriginCountry { get; set; }
    public int? FoundedYear { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
