using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces
{

    public interface IProductService
    {
        Task<PagedResultNew<ProductDto>> GetProductsAsync(ProductPagedRequest request);
        Task<ProductDto?> GetProductByIdAsync(Guid id);
        Task<List<ProductDto>> GetProductByWarehouseIdAsync(Guid id);
        Task<List<ProductDto>> GetProductByStoreIdAsync(Guid id);
        Task<ProductDetailsDto?> GetProductDetailsByIdAsync(Guid id);
        Task<List<ProductOptionDto>> GetProductOptionsAsync();
        Task<Guid> CreateProductAsync(ProductDetailsDto dto);
        Task<Guid> CreateProductDetailsAsync(ProductDetailsDto dto);
        Task UpdateProductAsync(ProductDetailsDto dto);
        Task UpdateProductDetailsAsync(ProductDetailsDto dto);
        Task DeleteProductAsync(Guid id);
        Task<ProductDetailViewDto?> GetProductDetailViewAsync(Guid id);
    }
}