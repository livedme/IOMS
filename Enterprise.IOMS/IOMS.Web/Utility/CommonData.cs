using IOMS.Application.DTOs;
using IOMS.Domain.Entities;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Web.Utility
{
    public class CommonData
    {
        public static List<CategoryDto> GetCategoryList(ApplicationDbContext dbContext)
        {
            var result = new List<CategoryDto>();

            var roots = dbContext.Categories
                .Include(c => c.SubCategories)
                .ThenInclude(c => c.SubCategories)
                .ThenInclude(c => c.SubCategories)
                .ThenInclude(c => c.SubCategories)
                .Include(c => c.Products)
                .Where(c => c.ParentCategoryId == null)
                .ToList();

            foreach (var root in roots)
                FlattenCategory(root, result, depth: 0);

            return result;
        }

        private static void FlattenCategory(Category category, List<CategoryDto> result, int depth)
        {
            var prefix = depth == 0 ? "" : category.ParentCategory?.Name + " > ";
            result.Add(new CategoryDto(
                Id: category.Id,
                Name: $"{prefix}{category.Name}",
                Description: category.Description,
                ParentCategoryId: category.ParentCategoryId,
                ParentCategoryName: category.ParentCategory?.Name,
                Path: $"{prefix}{category.Name}",
                ProductCount: category.Products?.Count ?? 0
            ));

            foreach (var child in category.SubCategories)
                FlattenCategory(child, result, depth + 1);
        }
    }
}
