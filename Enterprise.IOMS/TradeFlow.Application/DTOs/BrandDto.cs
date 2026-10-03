using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Application.DTOs
{
    public class BrandDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BrandCode { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? OriginCompany { get; set; }
        public string? OriginCountry { get; set; }
        public int? FoundedYear { get; set; }
        public int? ProductCount { get; set; }

        public BrandDto(Guid id, string name, string? brandCode, string? description, int? productCount)
        {
            Id = id;
            Name = name;
            BrandCode = brandCode;
            Description = description;
            ProductCount = productCount;
        }
    }

    public class CreateBrandDto
    {
        public string Name { get; set; } = string.Empty;
        public string? BrandCode { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string Status { get; set; } = "Active";
        public string? OriginCompany { get; set; }
        public string? OriginCountry { get; set; }
        public int? FoundedYear { get; set; }
    }


}
