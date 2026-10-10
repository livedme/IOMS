using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{
    // Products
    public record ProductDto(Guid Id, string Name, string? SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice, 
        decimal WholeSellingPrice,int ReorderStockLevel, int? MinOrderQuantity, int? WarrantyInMonths, Guid CategoryId, string? CategoryName,
        Guid? BrandId, string? BrandName, string? Model, string? ImageUrl,int TotalStock, bool IsKit, string? OriginCountry, 
        string? OriginManufacturer, List<ProductWarehouseStockDto>? WarehouseStocks = null, string? ParentCategoryName = null,
        string? SubCategoryName = null, int AvailableQuantity = 0, decimal? LastSalesRate = null);


    public record CreateProductDto(string Name, string? SKU, string? Barcode, string? Description, decimal CostPrice, decimal SellingPrice,
        decimal WholeSellingPrice,int ReorderStockLevel, int? MinOrderQuantity, int? WarrantyInMonths, Guid CategoryId,
        Guid? BrandId, Guid? BaseUoMId, string? ImageUrl);

    // Extended CreateProductDto
    public record CreateProductDetailsDto(Guid Id, string Name, string? SKU, string? Barcode, string? Description, decimal CostPrice,
        decimal SellingPrice,decimal WholeSellingPrice, int ReorderStockLevel, int? MinOrderQuantity, int? WarrantyInMonths, 
        Guid CategoryId, Guid? BrandId, Guid? BaseUoMId, string? ImageUrl, string? Model);

    public record UpdateProductDto(Guid Id, string Name, string? SKU, string? Barcode, string? Description, decimal CostPrice, 
        decimal SellingPrice,int ReorderStockLevel, int? MinOrderQuantity, int? WarrantyInMonths, Guid CategoryId, Guid? BrandId, 
        Guid? BaseUoMId, string? ImageUrl);
    public record UpdateProductDetailsDto(Guid Id, string Name, string? SKU, string? Barcode, string? Description, decimal CostPrice, 
        decimal SellingPrice,int ReorderStockLevel, int? MinOrderQuantity, int? WarrantyInMonths, Guid CategoryId, Guid? BrandId,
        Guid? BaseUoMId, string? ImageUrl, string? Model);
    public record ProductDetailViewDto(
    Guid Id,
    string Name,
    string? SKU,
    string? Barcode,
    string? Description,
    string? ImageUrl,
    decimal CostPrice,
    decimal SellingPrice,
    decimal WholeSellingPrice,
    int ReorderStockLevel,
    int? MinOrderQuantity,
    int? WarrantyInMonths,
    string? Model,
    string? OriginCountry,
    string? OriginManufacturer,
    string? CategoryName,
    string? ParentCategoryName,
    string? BrandName,
    string? PreferredSupplierName,
    int Last30DaysSales,
    int TotalAvailable,
    List<ProductWarehouseStockDto> WarehouseStock,
    List<StockMovementReportRowDto> Movements,
    List<ProductOrderLineDto> SalesOrderLines,
    List<ProductOrderLineDto> PurchaseOrderLines);

    public class ProductDetailsDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WholeSellingPrice { get; set; }
        public int ReorderStockLevel { get; set; }
        public int? MinOrderQuantity { get; set; }
        public int? WarrantyInMonths { get; set; }
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
        public string? OriginCountry { get; set; }
        public string? OriginManufacturer { get; set; }
    }

    /// <summary>
    /// The minimum a product picker needs. Ten-plus components were fetching the whole Product entity
    /// graph and materialising it in order to bind a dropdown that only ever reads the name.
    /// </summary>
    public record ProductOptionDto(Guid Id, string Name, string Sku, decimal SellingPrice, decimal CostPrice);


}
