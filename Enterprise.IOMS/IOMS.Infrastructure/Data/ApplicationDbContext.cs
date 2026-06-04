using System.Text.Json;
using IOMS.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace IOMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly Guid _tenantId;
    private readonly string? _userId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.GetTenantId();
        _userId = tenantProvider.GetUserId();
    }

    // Core
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<UoMConversion> UoMConversions => Set<UoMConversion>();

    // Orders
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    // Quotations
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesQuoteItem> SalesQuoteItems => Set<SalesQuoteItem>();
    public DbSet<RfqRequest> RfqRequests => Set<RfqRequest>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<RfqSupplierResponse> RfqSupplierResponses => Set<RfqSupplierResponse>();

    // Pricing & Tax
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<TaxJurisdiction> TaxJurisdictions => Set<TaxJurisdiction>();
    public DbSet<TaxExemption> TaxExemptions => Set<TaxExemption>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    // Accounting
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<DebitNote> DebitNotes => Set<DebitNote>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

    // Shipping & Logistics
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    // Stocktake
    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeItem> StocktakeItems => Set<StocktakeItem>();

    // Returns
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();

    // Kitting
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitComponent> KitComponents => Set<KitComponent>();

    // Landed Cost
    public DbSet<LandedCostComponent> LandedCostComponents => Set<LandedCostComponent>();
    public DbSet<LandedCostAllocation> LandedCostAllocations => Set<LandedCostAllocation>();

    // Workflow & Approvals
    public DbSet<ApprovalWorkflowRule> ApprovalWorkflowRules => Set<ApprovalWorkflowRule>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalRequestStep> ApprovalRequestSteps => Set<ApprovalRequestStep>();

    // Documents & Notifications
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    // Webhooks
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();

    // Custom Fields
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();

    // GDPR & Archival
    public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
    public DbSet<ArchivalPolicy> ArchivalPolicies => Set<ArchivalPolicy>();

    // Scheduled Reports
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global query filters for multi-tenancy and soft delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext).GetMethod(nameof(ConfigureGlobalFilters),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var generic = method!.MakeGenericMethod(entityType.ClrType);
                generic.Invoke(this, [builder]);
            }
        }

        // Decimal precision
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(6);
        }

        // Concurrency tokens
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.RowVersion))
                    .IsRowVersion();
            }
        }

        ConfigureRelationships(builder);
        ConfigureIndexes(builder);
    }

    private void ConfigureGlobalFilters<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);
    }

    private static void ConfigureRelationships(ModelBuilder builder)
    {
        builder.Entity<Brand>(b =>
        {
            b.Property(x => x.BrandCode).HasMaxLength(50);
            b.Property(x => x.LogoUrl).HasMaxLength(2048);
            b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Active");
            b.Property(x => x.OriginCompany).HasMaxLength(200);
            b.Property(x => x.OriginCountry).HasMaxLength(100);
        });

        // Category hierarchy
        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product-brand
        builder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        // Account hierarchy
        builder.Entity<Account>()
            .HasOne(a => a.ParentAccount)
            .WithMany(a => a.SubAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sales order items
        builder.Entity<SalesOrderItem>()
            .HasOne(i => i.SalesOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Purchase order items
        builder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Journal entry lines
        builder.Entity<JournalEntryLine>()
            .HasOne(l => l.JournalEntry)
            .WithMany(j => j.Lines)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JournalEntryLine>()
            .HasOne(l => l.Account)
            .WithMany(a => a.JournalEntryLines)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // UoM conversions
        builder.Entity<UoMConversion>()
            .HasOne(c => c.FromUoM)
            .WithMany(u => u.FromConversions)
            .HasForeignKey(c => c.FromUoMId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UoMConversion>()
            .HasOne(c => c.ToUoM)
            .WithMany(u => u.ToConversions)
            .HasForeignKey(c => c.ToUoMId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exchange rates
        builder.Entity<ExchangeRate>()
            .HasOne(e => e.FromCurrency)
            .WithMany(c => c.FromExchangeRates)
            .HasForeignKey(e => e.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExchangeRate>()
            .HasOne(e => e.ToCurrency)
            .WithMany(c => c.ToExchangeRates)
            .HasForeignKey(e => e.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invoice relationships - prevent cascading delete cycles
        builder.Entity<Invoice>()
            .HasOne(i => i.SalesOrder)
            .WithMany(o => o.Invoices)
            .HasForeignKey(i => i.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany(o => o.Invoices)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Invoice>()
            .HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Kit components
        builder.Entity<KitComponent>()
            .HasOne(kc => kc.Kit)
            .WithMany(k => k.Components)
            .HasForeignKey(kc => kc.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<KitComponent>()
            .HasOne(kc => kc.ComponentProduct)
            .WithMany()
            .HasForeignKey(kc => kc.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stocktake items
        builder.Entity<StocktakeItem>()
            .HasOne(si => si.Stocktake)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.StocktakeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Purchase return items
        builder.Entity<PurchaseReturnItem>()
            .HasOne(ri => ri.PurchaseReturn)
            .WithMany(r => r.Items)
            .HasForeignKey(ri => ri.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sales quote items
        builder.Entity<SalesQuoteItem>()
            .HasOne(qi => qi.SalesQuote)
            .WithMany(q => q.Items)
            .HasForeignKey(qi => qi.SalesQuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // RFQ items
        builder.Entity<RfqItem>()
            .HasOne(ri => ri.RfqRequest)
            .WithMany(r => r.Items)
            .HasForeignKey(ri => ri.RfqRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // RFQ supplier responses
        builder.Entity<RfqSupplierResponse>()
            .HasOne(rs => rs.RfqRequest)
            .WithMany(r => r.SupplierResponses)
            .HasForeignKey(rs => rs.RfqRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RfqSupplierResponse>()
            .HasOne(rs => rs.Supplier)
            .WithMany(s => s.RfqResponses)
            .HasForeignKey(rs => rs.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bank statement lines
        builder.Entity<BankStatementLine>()
            .HasOne(l => l.BankStatement)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.BankStatementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Webhook delivery logs
        builder.Entity<WebhookDeliveryLog>()
            .HasOne(l => l.Subscription)
            .WithMany(s => s.DeliveryLogs)
            .HasForeignKey(l => l.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Custom field values
        builder.Entity<CustomFieldValue>()
            .HasOne(v => v.Definition)
            .WithMany(d => d.Values)
            .HasForeignKey(v => v.DefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Approval request steps
        builder.Entity<ApprovalRequestStep>()
            .HasOne(s => s.ApprovalRequest)
            .WithMany(r => r.Steps)
            .HasForeignKey(s => s.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent multiple cascade paths for SalesOrder relationships
        builder.Entity<DeliveryNote>()
            .HasOne(dn => dn.SalesOrder)
            .WithMany(o => o.DeliveryNotes)
            .HasForeignKey(dn => dn.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Shipment>()
            .HasOne(s => s.SalesOrder)
            .WithMany(o => o.Shipments)
            .HasForeignKey(s => s.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Purchase returns
        builder.Entity<PurchaseReturn>()
            .HasOne(pr => pr.PurchaseOrder)
            .WithMany(po => po.Returns)
            .HasForeignKey(pr => pr.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // PriceList items
        builder.Entity<PriceListItem>()
            .HasOne(pi => pi.PriceList)
            .WithMany(pl => pl.Items)
            .HasForeignKey(pi => pi.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Inventory - prevent cascading deletes
        builder.Entity<Inventory>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Inventories)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Inventory>()
            .HasOne(i => i.Warehouse)
            .WithMany(w => w.Inventories)
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tax jurisdictions
        builder.Entity<TaxRate>()
            .HasOne(t => t.TaxJurisdiction)
            .WithMany(j => j.TaxRates)
            .HasForeignKey(t => t.TaxJurisdictionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Landed cost allocations
        builder.Entity<LandedCostAllocation>()
            .HasOne(a => a.Component)
            .WithMany(c => c.Allocations)
            .HasForeignKey(a => a.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIndexes(ModelBuilder builder)
    {
        builder.Entity<Product>().HasIndex(p => new { p.TenantId, p.SKU }).IsUnique();
        builder.Entity<Product>().HasIndex(p => p.Barcode);
        builder.Entity<Product>().HasIndex(p => p.Name);

        builder.Entity<Category>().HasIndex(c => new { c.TenantId, c.Name });
        builder.Entity<Brand>().HasIndex(b => new { b.TenantId, b.Name });
        builder.Entity<Brand>().HasIndex(b => new { b.TenantId, b.BrandCode });

        builder.Entity<Inventory>().HasIndex(i => new { i.ProductId, i.WarehouseId }).IsUnique();

        builder.Entity<Customer>().HasIndex(c => new { c.TenantId, c.Email });
        builder.Entity<Customer>().HasIndex(c => c.Name);

        builder.Entity<Supplier>().HasIndex(s => new { s.TenantId, s.Email });

        builder.Entity<SalesOrder>().HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.Entity<SalesOrder>().HasIndex(o => o.Status);
        builder.Entity<SalesOrder>().HasIndex(o => o.OrderDate);

        builder.Entity<PurchaseOrder>().HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.Entity<PurchaseOrder>().HasIndex(o => o.Status);

        builder.Entity<Account>().HasIndex(a => new { a.TenantId, a.Code }).IsUnique();

        builder.Entity<JournalEntry>().HasIndex(j => new { j.TenantId, j.Reference });
        builder.Entity<JournalEntry>().HasIndex(j => j.EntryDate);

        builder.Entity<Invoice>().HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        builder.Entity<Invoice>().HasIndex(i => i.DueDate);

        builder.Entity<SalesQuote>().HasIndex(q => new { q.TenantId, q.QuoteNumber }).IsUnique();

        builder.Entity<StockMovement>().HasIndex(m => m.MovementDate);
        builder.Entity<StockMovement>().HasIndex(m => m.ProductId);

        builder.Entity<AuditLog>().HasIndex(a => new { a.TenantId, a.TableName, a.RecordId });
        builder.Entity<AuditLog>().HasIndex(a => a.Timestamp);

        builder.Entity<Warehouse>().HasIndex(w => new { w.TenantId, w.Code }).IsUnique();

        builder.Entity<Currency>().HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.TenantId = _tenantId;
                        baseEntity.CreatedAt = DateTime.UtcNow;
                        baseEntity.CreatedBy = _userId ?? "system";
                        break;
                    case EntityState.Modified:
                        baseEntity.UpdatedAt = DateTime.UtcNow;
                        baseEntity.UpdatedBy = _userId ?? "system";
                        break;
                }
            }

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = _userId,
                TenantId = _tenantId
            };
            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "Created";
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        auditEntry.Action = "Deleted";
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.Action = "Updated";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken ct)
    {
        if (auditEntries.Count == 0) return;

        foreach (var entry in auditEntries)
        {
            foreach (var prop in entry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                    entry.RecordId = (Guid)prop.CurrentValue!;
                else
                    entry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
            }

            AuditLogs.Add(new AuditLog
            {
                TenantId = entry.TenantId,
                TableName = entry.TableName,
                RecordId = entry.RecordId,
                Action = entry.Action,
                UserId = entry.UserId,
                OldValues = entry.OldValues.Count == 0 ? null : JsonSerializer.Serialize(entry.OldValues),
                NewValues = entry.NewValues.Count == 0 ? null : JsonSerializer.Serialize(entry.NewValues),
                Timestamp = DateTime.UtcNow
            });
        }

        await base.SaveChangesAsync(ct);
    }
}

public class AuditEntry
{
    public AuditEntry(EntityEntry entry) => Entry = entry;
    public EntityEntry Entry { get; }
    public string TableName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RecordId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
}

public interface ITenantProvider
{
    Guid GetTenantId();
    string? GetUserId();
}
