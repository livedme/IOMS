using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Domain.Entities;

namespace IOMS.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.TotalStock, o => o.MapFrom(s => s.Inventories.Sum(i => i.Quantity)));

        // Product Details (extended attributes)
        CreateMap<Product, ProductDetailsDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.TotalStock, o => o.MapFrom(s => s.Inventories.Sum(i => i.Quantity)))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.Model, o => o.MapFrom(s => s.Model));

        // Brand
        CreateMap<Brand, BrandDto>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));

        // Branch
        CreateMap<Branch, BranchDto>();

        // Category
        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ParentCategoryName, o => o.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null))
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));

        // Warehouse
        CreateMap<Warehouse, WarehouseDto>();

        // Inventory
        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.AvailableQuantity, o => o.MapFrom(s => s.Quantity - s.ReservedQuantity));

        // Customer
        CreateMap<Customer, CustomerDto>();

        // Supplier
        CreateMap<Supplier, SupplierDto>();

        // Sales Order
        CreateMap<SalesOrder, SalesOrderDto>()
            .ForMember(d => d.Branch, o => o.MapFrom(s => new BranchDto(s.BranchId ?? Guid.Empty, s.Branch.Name, s.Branch.Code, s.Branch.Location, s.Branch.Address, s.Branch.IsActive)));

        CreateMap<SalesOrderItem, SalesOrderItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU));

        // Purchase Order
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ConstructUsing((s, ctx) => new PurchaseOrderDto(
                s.Id,
                s.OrderNumber,
                s.SupplierId,
                s.Supplier != null ? s.Supplier.SupplierName : string.Empty,
                s.WarehouseId ?? Guid.Empty,
                s.Warehouse != null ? s.Warehouse.Name : string.Empty,
                s.PurchaseDate,
                s.Status,
                s.SubTotal,
                s.LabourCharge,
                s.TruckCharge,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalAmount,
                s.TaxAmount,
                s.PaidAmount,
                s.DueAmount,
                s.Notes,
                s.ExpectedDeliveryDate,
                ctx.Mapper.Map<List<PurchaseOrderItemDto>>(s.Items)
            ));

        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ConstructUsing(s => new PurchaseOrderItemDto(
                s.Id,
                s.ProductId,
                s.Product != null ? s.Product.Name : string.Empty,
                s.Product != null ? s.Product.SKU : string.Empty,
                s.Quantity,
                s.ReceivedQuantity,
                s.UnitPrice,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalDiscount,
                s.LineTotal
            ));

        // Account
        CreateMap<Account, AccountDto>()
            .ConstructUsing(s => new AccountDto(s.Id, s.Code, s.Name, s.AccountType,
                s.ParentAccountId, s.ParentAccount != null ? s.ParentAccount.Name : null,
                s.IsActive, s.IsSystemAccount, s.Description, 0))
            .ForMember(d => d.ParentAccountName, o => o.MapFrom(s => s.ParentAccount != null ? s.ParentAccount.Name : null))
            .ForMember(d => d.Balance, o => o.Ignore());

        // Journal Entry
        CreateMap<JournalEntry, JournalEntryDto>();
        CreateMap<JournalEntryLine, JournalEntryLineDto>()
            .ForMember(d => d.AccountCode, o => o.MapFrom(s => s.Account.Code))
            .ForMember(d => d.AccountName, o => o.MapFrom(s => s.Account.Name));

        // Invoice
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer != null ? s.Customer.CustomerName : null))
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : null))
            .ForMember(d => d.BalanceDue, o => o.MapFrom(s => s.TotalAmount - s.PaidAmount));

        // Payment
        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.InvoiceNumber, o => o.MapFrom(s => s.Invoice.InvoiceNumber));

        // Sales Quote
        CreateMap<SalesQuote, SalesQuoteDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.CustomerName));

        CreateMap<SalesQuoteItem, SalesQuoteItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU));

        // Tax
        CreateMap<TaxRate, TaxRateDto>()
            .ForMember(d => d.JurisdictionName, o => o.MapFrom(s => s.TaxJurisdiction != null ? s.TaxJurisdiction.Name : null));

        CreateMap<TaxJurisdiction, TaxJurisdictionDto>();

        // PriceList
        CreateMap<PriceList, PriceListDto>()
            .ForMember(d => d.CurrencyCode, o => o.MapFrom(s => s.Currency != null ? s.Currency.Code : null));

        CreateMap<PriceListItem, PriceListItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU));

        // Currency
        CreateMap<Currency, CurrencyDto>();
        CreateMap<ExchangeRate, ExchangeRateDto>()
            .ForMember(d => d.FromCurrencyCode, o => o.MapFrom(s => s.FromCurrency.Code))
            .ForMember(d => d.ToCurrencyCode, o => o.MapFrom(s => s.ToCurrency.Code));

        // Shipping
        CreateMap<DeliveryNote, DeliveryNoteDto>()
            .ForMember(d => d.SalesOrderNumber, o => o.MapFrom(s => s.SalesOrder.OrderNumber));

        CreateMap<Shipment, ShipmentDto>();

        // Stocktake
        CreateMap<Stocktake, StocktakeDto>()
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.TotalItems, o => o.MapFrom(s => s.Items.Count))
            .ForMember(d => d.CountedItems, o => o.MapFrom(s => s.Items.Count(i => i.CountedQuantity.HasValue)));

        CreateMap<StocktakeItem, StocktakeItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU))
            .ForMember(d => d.Variance, o => o.MapFrom(s => (s.CountedQuantity ?? 0) - s.SystemQuantity));

        // Purchase Return
        CreateMap<PurchaseReturn, PurchaseReturnDto>()
            .ForMember(d => d.PurchaseOrderNumber, o => o.MapFrom(s => s.PurchaseOrder.OrderNumber));

        CreateMap<PurchaseReturnItem, PurchaseReturnItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        // RFQ
        CreateMap<RfqRequest, RfqRequestDto>();
        CreateMap<RfqItem, RfqItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        CreateMap<RfqSupplierResponse, RfqSupplierResponseDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : null));

        // Credit/Debit Notes
        CreateMap<CreditNote, CreditNoteDto>()
            .ForMember(d => d.InvoiceNumber, o => o.MapFrom(s => s.Invoice.InvoiceNumber));

        CreateMap<DebitNote, DebitNoteDto>()
            .ForMember(d => d.InvoiceNumber, o => o.MapFrom(s => s.Invoice.InvoiceNumber));

        // Audit Log
        CreateMap<AuditLog, AuditLogDto>();

        // Kit
        CreateMap<Kit, KitDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU));

        CreateMap<KitComponent, KitComponentDto>()
            .ForMember(d => d.ComponentProductName, o => o.MapFrom(s => s.ComponentProduct.Name))
            .ForMember(d => d.ComponentProductSKU, o => o.MapFrom(s => s.ComponentProduct.SKU));

        // UoM
        CreateMap<UnitOfMeasure, UnitOfMeasureDto>();
        CreateMap<UoMConversion, UoMConversionDto>()
            .ForMember(d => d.FromUoMName, o => o.MapFrom(s => s.FromUoM.Name))
            .ForMember(d => d.ToUoMName, o => o.MapFrom(s => s.ToUoM.Name));

        // Bank Reconciliation
        CreateMap<BankStatement, BankStatementDto>();
        CreateMap<BankStatementLine, BankStatementLineDto>();

        // Landed Cost
        CreateMap<LandedCostComponent, LandedCostComponentDto>();
        CreateMap<LandedCostAllocation, LandedCostAllocationDto>()
            .ForMember(d => d.PurchaseOrderNumber, o => o.MapFrom(s => s.PurchaseOrder != null ? s.PurchaseOrder.OrderNumber : null))
            .ForMember(d => d.ComponentName, o => o.MapFrom(s => s.Component.Name));

        // Document Template
        CreateMap<DocumentTemplate, DocumentTemplateDto>();

        // Approval
        CreateMap<ApprovalWorkflowRule, ApprovalWorkflowRuleDto>();
        CreateMap<ApprovalRequest, ApprovalRequestDto>();
        CreateMap<ApprovalRequestStep, ApprovalRequestStepDto>();

        // Webhook
        CreateMap<WebhookSubscription, WebhookSubscriptionDto>()
            .ForMember(d => d.DeliveryCount, o => o.MapFrom(s => s.DeliveryLogs.Count));
        CreateMap<WebhookDeliveryLog, WebhookDeliveryLogDto>();

        // Custom Field
        CreateMap<CustomFieldDefinition, CustomFieldDefinitionDto>();
        CreateMap<CustomFieldValue, CustomFieldValueDto>()
            .ForMember(d => d.FieldName, o => o.MapFrom(s => s.Definition.FieldName));

        // Data Privacy
        CreateMap<DataSubjectRequest, DataSubjectRequestDto>();

        // Archival
        CreateMap<ArchivalPolicy, ArchivalPolicyDto>();

        // Scheduled Report
        CreateMap<ScheduledReport, ScheduledReportDto>();

        // Notification
        CreateMap<NotificationLog, NotificationLogDto>();

        // Notification Template
        CreateMap<NotificationTemplate, NotificationTemplateDto>();

        // Product Serial
        CreateMap<ProductSerial, ProductSerialDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : null))
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : null))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));
    }
}
