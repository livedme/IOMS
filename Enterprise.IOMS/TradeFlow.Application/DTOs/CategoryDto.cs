using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{

    // Categories
    public record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentCategoryId, string? ParentCategoryName, int? ProductCount, string? Path);

    public record CreateCategoryDto(string Name, string? Description, Guid? ParentCategoryId);

}
