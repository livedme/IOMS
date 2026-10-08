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

// ─── Tax Service ────────────────────────────────────────────────────────────
public class TaxService : ITaxService
{
    private readonly ApplicationDbContext _context;

    public TaxService(ApplicationDbContext context) => _context = context;

    public async Task<List<TaxRateDto>> GetTaxRates()
    {
        return await _context.TaxRates
            .Include(t => t.TaxJurisdiction)
            .Select(t => new TaxRateDto(
                t.Id, t.Name, t.Rate, t.TaxType,
                t.TaxJurisdictionId, t.TaxJurisdiction != null ? t.TaxJurisdiction.Name : null,
                t.EffectiveFrom, t.EffectiveTo, t.IsActive, t.IsCompound))
            .ToListAsync();
    }

    public async Task<Guid> CreateTaxRate(CreateTaxRateDto dto)
    {
        var taxRate = new TaxRate
        {
            Name = dto.Name,
            Rate = dto.Rate,
            TaxType = dto.TaxType,
            TaxJurisdictionId = dto.TaxJurisdictionId,
            IsCompound = dto.IsCompound,
            IsActive = dto.IsActive,
            EffectiveFrom = DateTime.UtcNow
        };

        _context.TaxRates.Add(taxRate);
        await _context.SaveChangesAsync();
        return taxRate.Id;
    }

    public async Task UpdateTaxRate(Guid id, CreateTaxRateDto dto)
    {
        var taxRate = await _context.TaxRates.FindAsync(id)
            ?? throw new EntityNotFoundException("TaxRate", id);

        taxRate.Name = dto.Name;
        taxRate.Rate = dto.Rate;
        taxRate.TaxType = dto.TaxType;
        taxRate.TaxJurisdictionId = dto.TaxJurisdictionId;
        taxRate.IsCompound = dto.IsCompound;
        taxRate.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task<List<TaxJurisdictionDto>> GetJurisdictions()
    {
        return await _context.TaxJurisdictions
            .Include(j => j.TaxRates)
            .Select(j => new TaxJurisdictionDto(
                j.Id, j.Name, j.Code, j.Country, j.State,
                j.TaxRates.Select(t => new TaxRateDto(
                    t.Id, t.Name, t.Rate, t.TaxType,
                    t.TaxJurisdictionId, j.Name,
                    t.EffectiveFrom, t.EffectiveTo, t.IsActive, t.IsCompound)).ToList()))
            .ToListAsync();
    }

    public async Task<Guid> CreateJurisdiction(CreateTaxJurisdictionDto dto)
    {
        var jurisdiction = new TaxJurisdiction
        {
            Name = dto.Name,
            Code = dto.Code,
            Country = dto.Country,
            State = dto.State
        };

        _context.TaxJurisdictions.Add(jurisdiction);
        await _context.SaveChangesAsync();
        return jurisdiction.Id;
    }

    public async Task<decimal> CalculateTax(Guid jurisdictionId, decimal amount)
    {
        var rates = await _context.TaxRates
            .Where(t => t.TaxJurisdictionId == jurisdictionId && t.IsActive
                && t.EffectiveFrom <= DateTime.UtcNow
                && (t.EffectiveTo == null || t.EffectiveTo >= DateTime.UtcNow))
            .OrderBy(t => t.IsCompound)
            .ToListAsync();

        decimal totalTax = 0;
        decimal taxableAmount = amount;

        foreach (var rate in rates)
        {
            var tax = Math.Round(taxableAmount * rate.Rate / 100m, 2);
            totalTax += tax;
            if (rate.IsCompound) taxableAmount += tax;
        }

        return totalTax;
    }
}
