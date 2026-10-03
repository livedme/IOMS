using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IProductService
{
    //Task<PagedResult<ProductDto>> GetProductsAsync(string? search, Guid? categoryId, int page, int pageSize);
    Task<PagedResultNew<ProductDto>> GetProductsAsync(ProductPagedRequest request);
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<ProductDetailsDto?> GetProductDetailsByIdAsync(Guid id);

    /// <summary>
    /// Lightweight product list for pickers. Replaces a dozen <c>DbContext.Products.ToListAsync()</c>
    /// calls that pulled the full entity — including its tracked navigation state — to populate a
    /// dropdown that only ever shows the name and price.
    /// </summary>
    Task<List<ProductOptionDto>> GetProductOptionsAsync();

    Task<Guid> CreateProductAsync(ProductDetailsDto dto);
    Task<Guid> CreateProductDetailsAsync(ProductDetailsDto dto);
    Task UpdateProductAsync(ProductDetailsDto dto);
    Task UpdateProductDetailsAsync(ProductDetailsDto dto);
    Task DeleteProductAsync(Guid id);

    /// <summary>
    /// One round trip for the read-only detail screen: header, per-warehouse stock, recent
    /// movements and the sales and purchase lines. Replaces eight queries the page issued
    /// against its own context, including a tracked product graph.
    /// </summary>
    Task<ProductDetailViewDto?> GetProductDetailViewAsync(Guid id);
    }
