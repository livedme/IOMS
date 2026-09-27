using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ProductService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        //public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductPagedRequest request)
        //{
        //    var page = Math.Max(0, request.Page);
        //    var pageSize = Math.Clamp(request.PageSize, 1, 100);
        //    var search = request.SearchTerm?.Trim();
        //    var hasSearch = !string.IsNullOrWhiteSpace(search);
        //    var lowerSearch = hasSearch ? search!.ToLower() : null;

        //    var query = _db.Products.IgnoreQueryFilters.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Inventories).AsQueryable();
        //    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        //        query = query.Where(p => p.Name.Contains(request.SearchTerm) || p.SKU.Contains(request.SearchTerm));
        //    if (request.CategoryId.HasValue)
        //        query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        //    if (request.BrandId.HasValue)
        //        query = query.Where(p => p.BrandId == request.BrandId.Value);

        //    var total = await query.CountAsync();
        //    var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        //    return new PagedResult<ProductDto>(_mapper.Map<List<ProductDto>>(items), total, page, pageSize);
        //} 
        public async Task<PagedResultNew<ProductDto>> GetProductsAsync(ProductPagedRequest request)
        {
            var response = new PagedResultNew<ProductDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();
            var hasSearch = !string.IsNullOrWhiteSpace(search);
            var lowerSearch = hasSearch ? search!.ToLower() : null;

            IQueryable<Product> query = Enumerable.Empty<Product>().AsQueryable();

            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = _db.Products.IgnoreQueryFilters().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Inventories).AsNoTracking().Where(p => !p.IsDeleted && p.TenantId == request.TenantId.Value).AsQueryable();
            else
                query = _db.Products.IgnoreQueryFilters().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Inventories).AsNoTracking().Where(p => !p.IsDeleted).AsQueryable();



            response.Stats.Add("TotalCount", await query.CountAsync());
            response.Stats.Add("InStockCount", await query.Where(x => x.Inventories.Sum(i => i.Quantity) > x.ReorderStockLevel && x.Inventories.Sum(i => i.Quantity) > 0).Select(x => x.Inventories.Sum(i => i.Quantity)).SumAsync());
            response.Stats.Add("LowStockCount", await query.Where(x => x.Inventories.Sum(i => i.Quantity) > 0 && x.Inventories.Sum(i => i.Quantity) <= x.ReorderStockLevel).Select(x => x.Inventories.Sum(i => i.Quantity)).SumAsync());
            response.Stats.Add("OutOfStockCount", await query.Where(x => x.Inventories.Sum(i => i.Quantity) <= 0).Select(x => x.Inventories.Sum(i => i.Quantity)).SumAsync());


            //_statsCache = all.Items;

            //_totalCount = _totalCount == 0 ? all.TotalCount : _totalCount;
            //_inStockCount = _statsCache.Count(x => x.TotalStock > x.ReorderStockLevel && x.TotalStock > 0);
            //_lowStockCount = _statsCache.Count(x => x.TotalStock > 0 && x.TotalStock <= x.ReorderStockLevel);
            //_outOfStockCount = _statsCache.Count(x => x.TotalStock <= 0);
            //if (_inStockCount + _lowStockCount + _outOfStockCount == 0 && _totalCount > 0)
            //{
            //    _inStockCount = (int)(_totalCount * 0.2);
            //    _lowStockCount = (int)(_totalCount * 0.2);
            //    _outOfStockCount = (int)(_totalCount * 0.2);
            //}


            // var result = await ProductService.GetProductsAsync(_searchString, _selectedCategoryId, _page, _pageSize);

            if (request.Status == StockFilter.InStock)
                query = query.Where(x => x.Inventories.Sum(i => i.Quantity) > x.ReorderStockLevel && x.Inventories.Sum(i => i.Quantity) > 0);
            else if (request.Status == StockFilter.LowStock)
                query = query.Where(x => x.Inventories.Sum(i => i.Quantity) > 0 && x.Inventories.Sum(i => i.Quantity) <= x.ReorderStockLevel);
            else if (request.Status == StockFilter.OutOfStock)
                query = query.Where(x => x.Inventories.Sum(i => i.Quantity) <= 0);



            // CategoryId filter
            if (request.CategoryId != null && request.CategoryId != Guid.Empty)
            {
                query = query.Where(x => x.CategoryId == request.CategoryId);
            }
            // Branch filter
            if (request.BrandId != null && request.BrandId != Guid.Empty)
            {
                query = query.Where(x => x.BrandId == request.BrandId);
            }

            if (hasSearch)
            {
                query = query.Where(t =>
                    t.Name.ToLower().Contains(lowerSearch!) ||
                    t.SKU.ToLower().Contains(lowerSearch!) ||
                    t.Barcode.ToLower().Contains(lowerSearch!) ||
                    t.Description.ToLower().Contains(lowerSearch!) ||
                    t.Model.ToLower().Contains(lowerSearch!) ||
                    t.CostPrice.ToString().ToLower().Contains(lowerSearch!) ||
                    (t.SellingPrice != null && t.WholeSellingPrice.ToString().ToLower().Contains(lowerSearch!)) ||
                    t.WholeSellingPrice.ToString().ToLower().Contains(lowerSearch!));
            }

            var totalCount = await query.CountAsync();

            //// --- Stats: per-status counts for header cards (single GROUP BY, no full scan of rows) ---
            //var statsQuery = db.Trucks.IgnoreQueryFilters().AsNoTracking().Where(t => !t.IsDeleted).AsQueryable();
            //if (request.TenantId.HasValue && request.TenantId.Value != 0)
            //    statsQuery = statsQuery.Where(t => t.TenantId == request.TenantId.Value);
            //var statusGroups = await statsQuery.GroupBy(t => t.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync();
            //var stats = statusGroups.ToDictionary(x => x.Status, x => x.Count);
            //// add total under "All" key so UI can show filtered total vs global total distinction
            //stats["All"] = await statsQuery.CountAsync();

            //// Sorting — server-side ORDER BY before Skip/Take
            //var sortAsc = request.SortAscending;
            //query = request.SortColumn switch
            //{
            //    "VehicleName" => sortAsc ? query.OrderBy(x => x.VehicleName) : query.OrderByDescending(x => x.VehicleName),
            //    "TruckNo" => sortAsc ? query.OrderBy(x => x.TruckNo) : query.OrderByDescending(x => x.TruckNo),
            //    "RegistrationNo" => sortAsc ? query.OrderBy(x => x.RegistrationNo) : query.OrderByDescending(x => x.RegistrationNo),
            //    "VehicleType" => sortAsc ? query.OrderBy(x => x.VehicleType) : query.OrderByDescending(x => x.VehicleType),
            //    "Capacity" => sortAsc ? query.OrderBy(x => x.Capacity) : query.OrderByDescending(x => x.Capacity),
            //    "Status" => sortAsc ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status),
            //    "InsuranceExpiry" => sortAsc ? query.OrderBy(x => x.InsuranceExpiry) : query.OrderByDescending(x => x.InsuranceExpiry),
            //    "CreatedAt" => sortAsc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
            //    _ => sortAsc ? query.OrderBy(x => x.VehicleName) : query.OrderByDescending(x => x.VehicleName)
            //};

            var items = await query.Skip(page * pageSize).Take(pageSize)
                .Select(t => _mapper.Map<ProductDto>(t))
                .ToListAsync();


            response.Items = items;
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;

        }


        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            var product = await _db.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Inventories).FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDetailsDto?> GetProductDetailsByIdAsync(Guid id)
        {
            var product = await _db.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.Inventories).FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? null : _mapper.Map<ProductDetailsDto>(product);
        }

        public async Task<Guid> CreateProductAsync(ProductDetailsDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Barcode = dto.Barcode,

                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                Model = dto.Model,

                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                WholeSellingPrice = dto.WholeSellingPrice,

                ReorderStockLevel = dto.ReorderStockLevel,
                OriginManufacturer = dto.OriginManufacturer,
                OriginCountry = dto.OriginCountry,

                MinOrderQuantity = dto.MinOrderQuantity,
                BaseUoMId = dto.BaseUoMId,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product.Id;
        }

        public async Task<Guid> CreateProductDetailsAsync(ProductDetailsDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Barcode = dto.Barcode,

                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                Model = dto.Model,

                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                WholeSellingPrice = dto.WholeSellingPrice,

                ReorderStockLevel = dto.ReorderStockLevel,
                OriginManufacturer = dto.OriginManufacturer,
                OriginCountry = dto.OriginCountry,

                MinOrderQuantity = dto.MinOrderQuantity,
                BaseUoMId = dto.BaseUoMId,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product.Id;
        }

        public async Task UpdateProductAsync(ProductDetailsDto dto)
        {
            var product = await _db.Products.FindAsync(dto.Id) ?? throw new KeyNotFoundException("Product not found");
            product.Name = dto.Name;
            product.SKU = dto.SKU;
            product.Barcode = dto.Barcode;

            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.Model = dto.Model;

            product.CostPrice = dto.CostPrice;
            product.SellingPrice = dto.SellingPrice;
            product.WholeSellingPrice = dto.WholeSellingPrice;

            product.ReorderStockLevel = dto.ReorderStockLevel;
            product.OriginManufacturer = dto.OriginManufacturer;
            product.OriginCountry = dto.OriginCountry;

            product.MinOrderQuantity = dto.MinOrderQuantity;
            product.BaseUoMId = dto.BaseUoMId;
            product.ImageUrl = dto.ImageUrl;
            product.Description = dto.Description;


            await _db.SaveChangesAsync();
        }

        public async Task UpdateProductDetailsAsync(ProductDetailsDto dto)
        {
            var product = await _db.Products.FindAsync(dto.Id) ?? throw new KeyNotFoundException("Product not found");
            product.Name = dto.Name;
            product.SKU = dto.SKU;
            product.Barcode = dto.Barcode;

            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.Model = dto.Model;

            product.CostPrice = dto.CostPrice;
            product.SellingPrice = dto.SellingPrice;
            product.WholeSellingPrice = dto.WholeSellingPrice;

            product.ReorderStockLevel = dto.ReorderStockLevel;
            product.OriginManufacturer = dto.OriginManufacturer;
            product.OriginCountry = dto.OriginCountry;

            product.MinOrderQuantity = dto.MinOrderQuantity;
            product.BaseUoMId = dto.BaseUoMId;
            product.ImageUrl = dto.ImageUrl;
            product.Description = dto.Description;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id) ?? throw new KeyNotFoundException("Product not found");
            product.IsDeleted = true;
            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<BrandDto>> GetBrandsAsync(string? search, int page, int pageSize)
        {
            var query = _db.Brands.Include(b => b.Products).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Name.Contains(search)
                    || (b.BrandCode != null && b.BrandCode.Contains(search))
                    || (b.Description != null && b.Description.Contains(search))
                    || (b.OriginCompany != null && b.OriginCompany.Contains(search))
                    || (b.OriginCountry != null && b.OriginCountry.Contains(search)));

            var total = await query.CountAsync();
            var items = await query.OrderBy(b => b.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<BrandDto>(_mapper.Map<List<BrandDto>>(items), total, page, pageSize);
        }

        public async Task<List<BrandDto>> GetAllBrandsAsync()
        {
            var brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
            return _mapper.Map<List<BrandDto>>(brands);
        }
        public async Task<BrandDto> GetBrandByIdAsync(Guid id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) throw new KeyNotFoundException("Brand not found");
            return _mapper.Map<BrandDto>(brand);
        }
        public async Task<Guid> CreateBrandAsync(CreateBrandDto dto)
        {
            var brand = new Brand
            {
                Name = dto.Name,
                BrandCode = dto.BrandCode,
                Description = dto.Description,
                LogoUrl = dto.LogoUrl,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                OriginCompany = dto.OriginCompany,
                OriginCountry = dto.OriginCountry,
                FoundedYear = dto.FoundedYear
            };
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();
            return brand.Id;
        }
        public async Task<bool> UpdateBrandAsync(Guid id, CreateBrandDto dto)
        {
            var brand = await _db.Brands.FindAsync(id) ?? throw new KeyNotFoundException("Brand not found");
            brand.Name = dto.Name;
            brand.BrandCode = dto.BrandCode;
            brand.Description = dto.Description;
            brand.LogoUrl = dto.LogoUrl;
            brand.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status;
            brand.OriginCompany = dto.OriginCompany;
            brand.OriginCountry = dto.OriginCountry;
            brand.FoundedYear = dto.FoundedYear;
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteBrandAsync(Guid id)
        {
            var brand = await _db.Brands.FindAsync(id) ?? throw new KeyNotFoundException("Brand not found");
            brand.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(string? search, int page, int pageSize)
        {
            var query = _db.Categories.Include(c => c.ParentCategory).Include(c => c.Products).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<CategoryDto>(_mapper.Map<List<CategoryDto>>(items), total, page, pageSize);
        }


        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _db.Categories.Include(c => c.ParentCategory).Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<Guid> CreateCategoryAsync(CreateCategoryDto dto)
        {
            var category = new Category { Name = dto.Name, Description = dto.Description, ParentCategoryId = dto.ParentCategoryId };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category.Id;
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var category = await _db.Categories.FindAsync(id) ?? throw new KeyNotFoundException("Category not found");
            category.IsDeleted = true;
            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, int page, int pageSize)
        {
            var query = _db.Warehouses.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Name.Contains(search) || w.Code.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(w => w.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<WarehouseDto>(_mapper.Map<List<WarehouseDto>>(items), total, page, pageSize);
        }

        public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
        {
            var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            return _mapper.Map<List<WarehouseDto>>(warehouses);
        }

        public async Task<Guid> CreateWarehouseAsync(CreateWarehouseDto dto)
        {
            var warehouse = new Warehouse { Name = dto.Name, Code = dto.Code, Location = dto.Location, Address = dto.Address };
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            return warehouse.Id;
        }

        public async Task DeleteWarehouseAsync(Guid id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id) ?? throw new KeyNotFoundException("Warehouse not found");
            warehouse.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
