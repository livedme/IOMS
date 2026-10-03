using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto> GetCategoryByIdAsync(Guid id);
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(string? search, int page, int pageSize);
    Task<PagedResultNew<CategoryDto>> GetCategoriesPagedAsync(CategoryPagedRequest request);    
    Task<Guid> CreateCategoryAsync(CreateCategoryDto dto);
    Task<Guid> UpdateCategoryAsync(Guid id, CreateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);

}
