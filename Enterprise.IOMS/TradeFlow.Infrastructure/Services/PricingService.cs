using System.Diagnostics;
using System.Text;
using AutoMapper;
using Azure.Core;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace TradeFlow.Infrastructure.Services;

// ─── Pricing Service ────────────────────────────────────────────────────────
public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;

    public PricingService(ApplicationDbContext context) => _context = context;

    public async Task<List<PriceListDto>> GetPriceLists()
    {
        return await _context.PriceLists
            .Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Select(p => new PriceListDto(
                p.Id, p.Name, p.CurrencyId,
                p.Currency != null ? p.Currency.Code : null,
                p.IsDefault, p.EffectiveFrom, p.EffectiveTo, p.IsActive,
                p.Items.Select(i => new PriceListItemDto(
                    i.Id, i.ProductId, i.Product.Name, i.Product.SKU,
                    i.UnitPrice, i.MinQuantity)).ToList()))
            .ToListAsync();
    }

    public async Task<Guid> CreatePriceList(CreatePriceListDto dto)
    {
        var priceList = new PriceList
        {
            Name = dto.Name,
            CurrencyId = dto.CurrencyId,
            IsDefault = dto.IsDefault,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsActive = dto.IsActive
        };

        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
            {
                priceList.Items.Add(new PriceListItem
                {
                    ProductId = item.ProductId,
                    UnitPrice = item.UnitPrice,
                    MinQuantity = item.MinQuantity
                });
            }
        }

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();
        return priceList.Id;
    }

    public async Task AddPriceListItem(Guid priceListId, CreatePriceListItemDto dto)
    {
        var priceList = await _context.PriceLists.FindAsync(priceListId)
            ?? throw new EntityNotFoundException("PriceList", priceListId);

        _context.PriceListItems.Add(new PriceListItem
        {
            PriceListId = priceListId,
            ProductId = dto.ProductId,
            UnitPrice = dto.UnitPrice,
            MinQuantity = dto.MinQuantity
        });

        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetEffectivePrice(Guid productId, Guid? customerId, decimal quantity)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new EntityNotFoundException("Product", productId);

        // Check customer-specific price list
        if (customerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer?.DefaultPriceListId != null)
            {
                var item = await _context.PriceListItems
                    .Where(i => i.PriceListId == customer.DefaultPriceListId
                        && i.ProductId == productId
                        && (i.MinQuantity == null || i.MinQuantity <= (int)quantity))
                    .OrderByDescending(i => i.MinQuantity)
                    .FirstOrDefaultAsync();

                if (item != null) return item.UnitPrice;
            }
        }

        // Check default price list
        var defaultItem = await _context.PriceListItems
            .Include(i => i.PriceList)
            .Where(i => i.PriceList.IsDefault && i.PriceList.IsActive
                && i.ProductId == productId
                && (i.MinQuantity == null || i.MinQuantity <= (int)quantity))
            .OrderByDescending(i => i.MinQuantity)
            .FirstOrDefaultAsync();

        return defaultItem?.UnitPrice ?? product.SellingPrice;
    }

    public async Task<List<DiscountDto>> GetDiscounts()
    {
        return await _context.Discounts
            .Select(d => new DiscountDto(
                d.Id, d.Name, d.Type, d.Value,
                d.MinQuantity, d.MaxQuantity,
                d.StartDate, d.EndDate,
                d.ProductId, d.CategoryId, d.CustomerId, d.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateDiscount(CreateDiscountDto dto)
    {
        var discount = new Discount
        {
            Name = dto.Name,
            Type = dto.DiscountType,
            Value = dto.Value,
            MinQuantity = dto.MinQuantity,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            ProductId = dto.ProductId,
            CustomerId = dto.CustomerId
        };

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();
        return discount.Id;
    }

    public async Task<List<CurrencyDto>> GetCurrencies()
    {
        return await _context.Currencies
            .Select(c => new CurrencyDto(
                c.Id, c.Code, c.Name, c.Symbol,
                c.DecimalPlaces, c.IsBaseCurrency, c.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateCurrency(CreateCurrencyDto dto)
    {
        var currency = new Currency
        {
            Code = dto.Code,
            Name = dto.Name,
            Symbol = dto.Symbol,
            IsBaseCurrency = dto.IsBaseCurrency
        };

        _context.Currencies.Add(currency);
        await _context.SaveChangesAsync();
        return currency.Id;
    }

    public async Task<decimal> ConvertCurrency(decimal amount, Guid fromCurrencyId, Guid toCurrencyId)
    {
        if (fromCurrencyId == toCurrencyId) return amount;

        var rate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyId == fromCurrencyId && r.ToCurrencyId == toCurrencyId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (rate != null) return Math.Round(amount * rate.Rate, 2);

        // Try reverse
        var reverseRate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyId == toCurrencyId && r.ToCurrencyId == fromCurrencyId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (reverseRate != null && reverseRate.Rate != 0)
            return Math.Round(amount / reverseRate.Rate, 2);

        throw new DomainException("Exchange rate not found for the specified currencies.");
    }
}
