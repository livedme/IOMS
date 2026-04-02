using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> GetProducts(string? search, Guid? categoryId, int page, int pageSize)
    {
        var query = _db.Products.Include(p => p.Category).Include(p => p.Inventories).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<ProductDto>(_mapper.Map<List<ProductDto>>(items), total, page, pageSize);
    }

    public async Task<ProductDto?> GetProductById(Guid id)
    {
        var product = await _db.Products.Include(p => p.Category).Include(p => p.Inventories).FirstOrDefaultAsync(p => p.Id == id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<Guid> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name, SKU = dto.SKU, Barcode = dto.Barcode, Description = dto.Description,
            CostPrice = dto.CostPrice, SellingPrice = dto.SellingPrice, ReorderLevel = dto.ReorderLevel,
            MinimumOrderQuantity = dto.MinimumOrderQuantity, CategoryId = dto.CategoryId,
            BaseUoMId = dto.BaseUoMId, ImageUrl = dto.ImageUrl
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateProduct(UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(dto.Id) ?? throw new KeyNotFoundException("Product not found");
        product.Name = dto.Name; product.SKU = dto.SKU; product.Barcode = dto.Barcode;
        product.Description = dto.Description; product.CostPrice = dto.CostPrice;
        product.SellingPrice = dto.SellingPrice; product.ReorderLevel = dto.ReorderLevel;
        product.MinimumOrderQuantity = dto.MinimumOrderQuantity; product.CategoryId = dto.CategoryId;
        product.BaseUoMId = dto.BaseUoMId; product.ImageUrl = dto.ImageUrl;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteProduct(Guid id)
    {
        var product = await _db.Products.FindAsync(id) ?? throw new KeyNotFoundException("Product not found");
        product.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<CategoryDto>> GetCategories(string? search, int page, int pageSize)
    {
        var query = _db.Categories.Include(c => c.ParentCategory).Include(c => c.Products).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<CategoryDto>(_mapper.Map<List<CategoryDto>>(items), total, page, pageSize);
    }

    public async Task<List<CategoryDto>> GetAllCategories()
    {
        var categories = await _db.Categories.Include(c => c.ParentCategory).Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
        return _mapper.Map<List<CategoryDto>>(categories);
    }

    public async Task<Guid> CreateCategory(CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name, Description = dto.Description, ParentCategoryId = dto.ParentCategoryId };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category.Id;
    }

    public async Task DeleteCategory(Guid id)
    {
        var category = await _db.Categories.FindAsync(id) ?? throw new KeyNotFoundException("Category not found");
        category.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<WarehouseDto>> GetWarehouses(string? search, int page, int pageSize)
    {
        var query = _db.Warehouses.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Name.Contains(search) || w.Code.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderBy(w => w.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<WarehouseDto>(_mapper.Map<List<WarehouseDto>>(items), total, page, pageSize);
    }

    public async Task<List<WarehouseDto>> GetAllWarehouses()
    {
        var warehouses = await _db.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
        return _mapper.Map<List<WarehouseDto>>(warehouses);
    }

    public async Task<Guid> CreateWarehouse(CreateWarehouseDto dto)
    {
        var warehouse = new Warehouse { Name = dto.Name, Code = dto.Code, Location = dto.Location, Address = dto.Address };
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();
        return warehouse.Id;
    }

    public async Task DeleteWarehouse(Guid id)
    {
        var warehouse = await _db.Warehouses.FindAsync(id) ?? throw new KeyNotFoundException("Warehouse not found");
        warehouse.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}

public class CustomerSupplierService : ICustomerSupplierService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public CustomerSupplierService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomers(string? search, int page, int pageSize)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search)));
        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<CustomerDto>(_mapper.Map<List<CustomerDto>>(items), total, page, pageSize);
    }

    public async Task<CustomerDto?> GetCustomerById(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<Guid> CreateCustomer(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name, Email = dto.Email, Phone = dto.Phone, Address = dto.Address,
            City = dto.City, State = dto.State, Country = dto.Country, PostalCode = dto.PostalCode,
            CreditLimit = dto.CreditLimit, PaymentTerms = dto.PaymentTerms,
            TaxJurisdictionId = dto.TaxJurisdictionId, DefaultPriceListId = dto.DefaultPriceListId,
            DefaultCurrencyId = dto.DefaultCurrencyId, IsTaxExempt = dto.IsTaxExempt
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer.Id;
    }

    public async Task UpdateCustomer(Guid id, CreateCustomerDto dto)
    {
        var customer = await _db.Customers.FindAsync(id) ?? throw new KeyNotFoundException("Customer not found");
        customer.Name = dto.Name; customer.Email = dto.Email; customer.Phone = dto.Phone;
        customer.Address = dto.Address; customer.City = dto.City; customer.State = dto.State;
        customer.Country = dto.Country; customer.PostalCode = dto.PostalCode;
        customer.CreditLimit = dto.CreditLimit; customer.PaymentTerms = dto.PaymentTerms;
        customer.IsTaxExempt = dto.IsTaxExempt;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCustomer(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id) ?? throw new KeyNotFoundException("Customer not found");
        customer.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<SupplierDto>> GetSuppliers(string? search, int page, int pageSize)
    {
        var query = _db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search) || (s.Email != null && s.Email.Contains(search)));
        var total = await query.CountAsync();
        var items = await query.OrderBy(s => s.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<SupplierDto>(_mapper.Map<List<SupplierDto>>(items), total, page, pageSize);
    }

    public async Task<SupplierDto?> GetSupplierById(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<Guid> CreateSupplier(CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Name = dto.Name, Email = dto.Email, Phone = dto.Phone, Address = dto.Address,
            City = dto.City, State = dto.State, Country = dto.Country, PostalCode = dto.PostalCode,
            PaymentTerms = dto.PaymentTerms, DefaultCurrencyId = dto.DefaultCurrencyId,
            LeadTimeDays = dto.LeadTimeDays
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return supplier.Id;
    }

    public async Task UpdateSupplier(Guid id, CreateSupplierDto dto)
    {
        var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException("Supplier not found");
        supplier.Name = dto.Name; supplier.Email = dto.Email; supplier.Phone = dto.Phone;
        supplier.Address = dto.Address; supplier.City = dto.City; supplier.State = dto.State;
        supplier.Country = dto.Country; supplier.PostalCode = dto.PostalCode;
        supplier.PaymentTerms = dto.PaymentTerms; supplier.LeadTimeDays = dto.LeadTimeDays;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSupplier(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException("Supplier not found");
        supplier.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ISharedNumberGenerator _numberGen;

    public InvoiceService(AppDbContext db, IMapper mapper, ISharedNumberGenerator numberGen)
    {
        _db = db;
        _mapper = mapper;
        _numberGen = numberGen;
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoices(string? search, InvoiceStatus? status, InvoiceType? type, int page, int pageSize)
    {
        var query = _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.InvoiceNumber.Contains(search));
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (type.HasValue) query = query.Where(i => i.InvoiceType == type.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(i => i.InvoiceDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<InvoiceDto>(_mapper.Map<List<InvoiceDto>>(items), total, page, pageSize);
    }

    public async Task<InvoiceDto?> GetInvoiceById(Guid id)
    {
        var invoice = await _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).FirstOrDefaultAsync(i => i.Id == id);
        return invoice == null ? null : _mapper.Map<InvoiceDto>(invoice);
    }

    public async Task<Guid> CreateInvoice(CreateInvoiceDto dto)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = _numberGen.Generate("INV"),
            InvoiceType = dto.InvoiceType,
            SalesOrderId = dto.SalesOrderId,
            PurchaseOrderId = dto.PurchaseOrderId,
            CustomerId = dto.CustomerId,
            SupplierId = dto.SupplierId,
            DueDate = dto.DueDate,
            Notes = dto.Notes
        };

        if (dto.SalesOrderId.HasValue)
        {
            var so = await _db.SalesOrders.FindAsync(dto.SalesOrderId.Value);
            if (so != null) { invoice.SubTotal = so.SubTotal; invoice.TaxAmount = so.TaxAmount; invoice.TotalAmount = so.TotalAmount; invoice.CustomerId = so.CustomerId; }
        }
        else if (dto.PurchaseOrderId.HasValue)
        {
            var po = await _db.PurchaseOrders.FindAsync(dto.PurchaseOrderId.Value);
            if (po != null) { invoice.SubTotal = po.SubTotal; invoice.TaxAmount = po.TaxAmount; invoice.TotalAmount = po.TotalAmount; invoice.SupplierId = po.SupplierId; }
        }

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task<Guid> GenerateInvoiceFromSalesOrder(Guid salesOrderId)
    {
        var so = await _db.SalesOrders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == salesOrderId)
            ?? throw new KeyNotFoundException("Sales order not found");
        return await CreateInvoice(new CreateInvoiceDto(InvoiceType.Sales, salesOrderId, null, so.CustomerId, null, DateTime.UtcNow.AddDays(30), null));
    }

    public async Task<Guid> GenerateInvoiceFromPurchaseOrder(Guid purchaseOrderId)
    {
        var po = await _db.PurchaseOrders.Include(o => o.Supplier).FirstOrDefaultAsync(o => o.Id == purchaseOrderId)
            ?? throw new KeyNotFoundException("Purchase order not found");
        return await CreateInvoice(new CreateInvoiceDto(InvoiceType.Purchase, null, purchaseOrderId, null, po.SupplierId, DateTime.UtcNow.AddDays(30), null));
    }
}

public class UoMService : IUoMService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public UoMService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<UnitOfMeasureDto>> GetUnitsOfMeasure()
    {
        var items = await _db.UnitsOfMeasure.OrderBy(u => u.Name).ToListAsync();
        return _mapper.Map<List<UnitOfMeasureDto>>(items);
    }

    public async Task<Guid> CreateUnitOfMeasure(string name, string abbreviation, bool isBaseUnit)
    {
        var uom = new UnitOfMeasure { Name = name, Abbreviation = abbreviation, IsBaseUnit = isBaseUnit };
        _db.UnitsOfMeasure.Add(uom);
        await _db.SaveChangesAsync();
        return uom.Id;
    }

    public async Task DeleteUnitOfMeasure(Guid id)
    {
        var uom = await _db.UnitsOfMeasure.FindAsync(id) ?? throw new KeyNotFoundException("UoM not found");
        uom.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<UoMConversionDto>> GetConversions()
    {
        var items = await _db.UoMConversions.Include(c => c.FromUoM).Include(c => c.ToUoM).ToListAsync();
        return _mapper.Map<List<UoMConversionDto>>(items);
    }

    public async Task<Guid> CreateConversion(Guid fromUoMId, Guid toUoMId, decimal conversionFactor)
    {
        var conversion = new UoMConversion { FromUoMId = fromUoMId, ToUoMId = toUoMId, ConversionFactor = conversionFactor };
        _db.UoMConversions.Add(conversion);
        await _db.SaveChangesAsync();
        return conversion.Id;
    }

    public async Task DeleteConversion(Guid id)
    {
        var conversion = await _db.UoMConversions.FindAsync(id) ?? throw new KeyNotFoundException("Conversion not found");
        conversion.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ExchangeRateService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<ExchangeRateDto>> GetExchangeRates()
    {
        var items = await _db.ExchangeRates.Include(e => e.FromCurrency).Include(e => e.ToCurrency)
            .OrderByDescending(e => e.EffectiveDate).ToListAsync();
        return _mapper.Map<List<ExchangeRateDto>>(items);
    }

    public async Task<Guid> CreateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, DateTime effectiveDate)
    {
        var er = new ExchangeRate { FromCurrencyId = fromCurrencyId, ToCurrencyId = toCurrencyId, Rate = rate, EffectiveDate = effectiveDate };
        _db.ExchangeRates.Add(er);
        await _db.SaveChangesAsync();
        return er.Id;
    }

    public async Task DeleteExchangeRate(Guid id)
    {
        var er = await _db.ExchangeRates.FindAsync(id) ?? throw new KeyNotFoundException("Exchange rate not found");
        er.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public NotificationTemplateService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<NotificationTemplateDto>> GetTemplates()
    {
        var items = await _db.NotificationTemplates.OrderBy(t => t.EventType).ToListAsync();
        return _mapper.Map<List<NotificationTemplateDto>>(items);
    }

    public async Task<Guid> CreateTemplate(CreateNotificationTemplateDto dto)
    {
        var template = new NotificationTemplate { EventType = dto.EventType, Channel = dto.Channel, Subject = dto.Subject, BodyTemplate = dto.BodyTemplate };
        _db.NotificationTemplates.Add(template);
        await _db.SaveChangesAsync();
        return template.Id;
    }

    public async Task UpdateTemplate(Guid id, CreateNotificationTemplateDto dto)
    {
        var template = await _db.NotificationTemplates.FindAsync(id) ?? throw new KeyNotFoundException("Template not found");
        template.EventType = dto.EventType; template.Channel = dto.Channel;
        template.Subject = dto.Subject; template.BodyTemplate = dto.BodyTemplate;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTemplate(Guid id)
    {
        var template = await _db.NotificationTemplates.FindAsync(id) ?? throw new KeyNotFoundException("Template not found");
        template.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<List<UserDto>> GetUsers(string? search)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList()));
        }
        return result;
    }

    public async Task<UserDto?> GetUserById(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList());
    }

    public async Task ToggleUserActive(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
    }

    public async Task<List<string>> GetRoles() => await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

    public async Task<List<string>> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task AssignRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
        if (!await _roleManager.RoleExistsAsync(role)) throw new InvalidOperationException($"Role '{role}' does not exist");
        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task RemoveRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
        await _userManager.RemoveFromRoleAsync(user, role);
    }

    public async Task CreateRole(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName)) throw new InvalidOperationException($"Role '{roleName}' already exists");
        await _roleManager.CreateAsync(new IdentityRole(roleName));
    }

    public async Task DeleteRole(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName) ?? throw new KeyNotFoundException("Role not found");
        await _roleManager.DeleteAsync(role);
    }
}

public interface ISharedNumberGenerator
{
    string Generate(string prefix);
}

public class SharedNumberGenerator : ISharedNumberGenerator
{
    public string Generate(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
}
