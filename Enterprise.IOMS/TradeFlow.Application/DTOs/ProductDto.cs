using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{
    // Products
    public record ProductDto(Guid Id, string Name, string SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice, decimal WholeSellingPrice,
        int ReorderStockLevel, int MinOrderQuantity, Guid CategoryId, string? CategoryName, Guid? BrandId, string? BrandName, string? Model, string? ImageUrl,
        int TotalStock, bool IsKit, string OriginCountry, string OriginManufacturer);


    public record CreateProductDto(string Name, string SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice, decimal WholeSellingPrice,
        int ReorderStockLevel, int MinOrderQuantity, Guid CategoryId, Guid? BrandId, Guid? BaseUoMId, string? ImageUrl);

    // Extended CreateProductDto
    public record CreateProductDetailsDto(Guid Id, string Name, string SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice, 
        decimal WholeSellingPrice, int ReorderStockLevel, int MinOrderQuantity, Guid CategoryId, Guid? BrandId, Guid? BaseUoMId, string? ImageUrl, string? Model);

    public record UpdateProductDto(Guid Id, string Name, string SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice,
        int ReorderStockLevel, int MinOrderQuantity, Guid CategoryId, Guid? BrandId, Guid? BaseUoMId, string? ImageUrl);
    // Extended UpdateProductDto
    public record UpdateProductDetailsDto(Guid Id, string Name, string SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice,
        int ReorderStockLevel, int MinOrderQuantity, Guid CategoryId, Guid? BrandId, Guid? BaseUoMId, string? ImageUrl, string? Model);


    public class ProductDetailsDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string SKU { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WholeSellingPrice { get; set; }
        public int ReorderStockLevel { get; set; }
        public int MinOrderQuantity { get; set; }
        public ControlDto CategoryControl { get; set; } = new ControlDto();
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ControlDto BrandControl { get; set; } = new ControlDto();
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalStock { get; set; }
        public bool IsKit { get; set; }
        public string? Model { get; set; }
        public Guid? BaseUoMId { get; set; }
        public string OriginCountry { get; set; }
        public string OriginManufacturer { get; set; }
    }

}
