using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Infrastructure.Data;
using IOMS.Infrastructure.Repositories;
using IOMS.Infrastructure.Services;
using IOMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace IOMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    b.MinBatchSize(4);
                    b.MaxBatchSize(100);
                }));

        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }

        services.AddScoped<ITenantCache, TenantCache>();

        //services.AddAuthentication(options =>
        //{
        //    options.DefaultScheme = IdentityConstants.ApplicationScheme;
        //    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        //}).AddIdentityCookies();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Application Services
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IStocktakeService, StocktakeService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
        services.AddScoped<IRfqService, RfqService>();
        services.AddScoped<ICreditDebitNoteService, CreditDebitNoteService>();
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();
        services.AddScoped<IKitService, KitService>();
        services.AddScoped<ILandedCostService, LandedCostService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<ICustomFieldService, CustomFieldService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IScheduledReportService, ScheduledReportService>();
        services.AddScoped<IDataPrivacyService, DataPrivacyService>();
        services.AddScoped<IArchivalService, ArchivalService>();
        services.AddScoped<IBranchService, BranchService>();
        // Entity CRUD Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductSerialService, ProductSerialService>();
        services.AddScoped<ICustomerSupplierService, CustomerSupplierService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IUoMService, UoMService>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddSingleton<ISharedNumberGenerator, SharedNumberGenerator>();

        return services;
    }
}
