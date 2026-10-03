using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllBrandsAsync();
    Task<BrandDto> GetBrandByIdAsync(Guid id); 
    Task<PagedResult<BrandDto>> GetBrandsAsync(string? search, int page, int pageSize);
    Task<PagedResultNew<BrandDto>> GetBrandsPagedAsync(BrandPagedRequest request);
    Task<Guid> CreateBrandAsync(CreateBrandDto dto);
    Task<bool> UpdateBrandAsync(Guid id, CreateBrandDto dto);
    Task<bool> DeleteBrandAsync(Guid id);
    }
