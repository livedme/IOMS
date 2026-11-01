using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;

namespace TradeFlow.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IMapper _mapper;
        private readonly ISharedNumberGenerator _numberGen;

        public InvoiceService(IDbContextFactory<ApplicationDbContext> dbFactory, IMapper mapper, ISharedNumberGenerator numberGen)
        {
            _dbFactory = dbFactory;
            _mapper = mapper;
            _numberGen = numberGen;
        }

        public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, InvoiceStatus? status, InvoiceType? type, int page, int pageSize)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.InvoiceNumber.Contains(search));
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);
            if (type.HasValue) query = query.Where(i => i.InvoiceType == type.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(i => i.InvoiceDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<InvoiceDto>(_mapper.Map<List<InvoiceDto>>(items), total, page, pageSize);
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var invoice = await _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).FirstOrDefaultAsync(i => i.Id == id);
            return invoice == null ? null : _mapper.Map<InvoiceDto>(invoice);
        }

        /// <summary>
        /// The invoice itself carries no line items — they live on the sales or purchase
        /// order it was generated from, so pull the linked order with its items and party info.
        /// </summary>
        public async Task<InvoiceDetailDto?> GetInvoiceDetailAsync(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var invoice = await _db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.SalesOrder).ThenInclude(o => o!.Items).ThenInclude(it => it.Product)
                .Include(i => i.SalesOrder).ThenInclude(o => o!.Items).ThenInclude(it => it.UoM)
                .Include(i => i.PurchaseOrder).ThenInclude(o => o!.Items).ThenInclude(it => it.Product)
                .Include(i => i.PurchaseOrder).ThenInclude(o => o!.Items).ThenInclude(it => it.UoM)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return null;

            List<InvoiceItemLineDto> items;
            string? orderNumber = null;
            DateTime? orderDate = null;
            if (invoice.InvoiceType == InvoiceType.Sales && invoice.SalesOrder != null)
            {
                orderNumber = invoice.SalesOrder.OrderNumber;
                orderDate = invoice.SalesOrder.OrderDate;
                items = invoice.SalesOrder.Items
                    .Select(it => new InvoiceItemLineDto(it.ProductId, it.Product.Name, it.Product.SKU, it.Quantity, it.UnitPrice, it.LineTotalPrice))
                    .ToList();
            }
            else if (invoice.InvoiceType == InvoiceType.Purchase && invoice.PurchaseOrder != null)
            {
                orderNumber = invoice.PurchaseOrder.OrderNumber;
                orderDate = invoice.PurchaseOrder.PurchaseDate;
                items = invoice.PurchaseOrder.Items
                    .Select(it => new InvoiceItemLineDto(it.ProductId, it.Product.Name, it.Product.SKU, it.Quantity, it.UnitPrice, it.LineTotal))
                    .ToList();
            }
            else
            {
                items = new List<InvoiceItemLineDto>();
            }

            var dto = _mapper.Map<InvoiceDto>(invoice);
            return new InvoiceDetailDto(
                dto,
                orderNumber,
                orderDate,
                invoice.Customer?.PaymentTerms ?? invoice.Supplier?.PaymentTerms,
                invoice.Customer?.CustomerPhone ?? invoice.Supplier?.SupplierPhone,
                invoice.Customer?.CustomerEmail ?? invoice.Supplier?.SupplierEmail,
                invoice.Customer?.Address ?? invoice.Supplier?.Address,
                invoice.Notes,
                items);
        }

        /// <summary>Invoices for the credit- and debit-note pickers, which only need number and amount.</summary>
        public async Task<List<InvoiceOptionDto>> GetRecentInvoiceOptions(int count, InvoiceType type)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            return await _db.Invoices.Where(i => i.InvoiceType == type && (i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Paid))
                .AsNoTracking()
                .OrderByDescending(i => i.InvoiceDate)
                .Take(count)
                .Select(i => new InvoiceOptionDto(i.Id, i.InvoiceNumber, i.TotalAmount, i.PaidAmount, i.BalanceDue))
                .ToListAsync();
        }

        public async Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            // One invoice per order: skip cancelled ones so a duplicate generate call
            // returns the existing invoice instead of minting a second one.
            var existing = dto.SalesOrderId.HasValue
                ? await _db.Invoices.FirstOrDefaultAsync(i => i.SalesOrderId == dto.SalesOrderId.Value && i.Status != InvoiceStatus.Cancelled)
                : dto.PurchaseOrderId.HasValue
                    ? await _db.Invoices.FirstOrDefaultAsync(i => i.PurchaseOrderId == dto.PurchaseOrderId.Value && i.Status != InvoiceStatus.Cancelled)
                    : null;
            if (existing != null) return existing.Id;

            var InvoiceNumber=string.Empty;

            var invoice = new Invoice
            {
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
                InvoiceNumber = NumberGenerator.GenerateInvoiceNumber("SO");
                var so = await _db.SalesOrders.FindAsync(dto.SalesOrderId.Value);
                if (so != null)
                {
                    invoice.SubTotal = so.SubTotal; invoice.TaxAmount = so.TaxAmount; invoice.TotalAmount = so.TotalAmount; invoice.CustomerId = so.CustomerId;
                    ApplyOrderPayment(so.PaidAmount);
                }
            }
            else if (dto.PurchaseOrderId.HasValue)
            {
                InvoiceNumber = NumberGenerator.GenerateInvoiceNumber("PO");

                var po = await _db.PurchaseOrders.FindAsync(dto.PurchaseOrderId.Value);
                if (po != null)
                {
                    invoice.SubTotal = po.SubTotal; invoice.TaxAmount = po.TaxAmount; invoice.TotalAmount = po.TotalAmount; invoice.SupplierId = po.SupplierId;
                    ApplyOrderPayment(po.PaidAmount);
                }
            }

            invoice.InvoiceNumber = InvoiceNumber;
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice.Id;

            void ApplyOrderPayment(decimal paidOnOrder)
            {
                invoice.PaidAmount = paidOnOrder;
                if (invoice.TotalAmount > 0 && invoice.PaidAmount >= invoice.TotalAmount)
                    invoice.Status = InvoiceStatus.Paid;
                else if (invoice.PaidAmount > 0)
                    invoice.Status = InvoiceStatus.PartiallyPaid;
            }
        }

        public async Task<Guid> GenerateInvoiceFromSalesOrderAsync(Guid salesOrderId)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var so = await _db.SalesOrders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == salesOrderId)
                ?? throw new KeyNotFoundException("Sales order not found");
            return await CreateInvoiceAsync(new CreateInvoiceDto(InvoiceType.Sales, salesOrderId, null, so.CustomerId, null, DateTime.UtcNow.AddDays(30), null));
        }

        public async Task<Guid> GenerateInvoiceFromPurchaseOrderAsync(Guid purchaseOrderId)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();
            var po = await _db.PurchaseOrders.Include(o => o.Supplier).FirstOrDefaultAsync(o => o.Id == purchaseOrderId)
                ?? throw new KeyNotFoundException("Purchase order not found");
            return await CreateInvoiceAsync(new CreateInvoiceDto(InvoiceType.Purchase, null, purchaseOrderId, null, po.SupplierId, DateTime.UtcNow.AddDays(30), null));
        }
    }

}
