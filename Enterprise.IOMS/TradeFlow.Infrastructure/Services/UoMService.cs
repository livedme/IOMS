using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Infrastructure.Services
{
    public class UoMService : IUoMService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IMapper _mapper;

        public UoMService(IDbContextFactory<ApplicationDbContext> dbFactory, IMapper mapper) { _dbFactory = dbFactory; _mapper = mapper; }

        public async Task<List<UnitOfMeasureDto>> GetUnitsOfMeasure()
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var items = await _db.UnitsOfMeasure.OrderBy(u => u.Name).ToListAsync();
            return _mapper.Map<List<UnitOfMeasureDto>>(items);
        }

        public async Task<Guid> CreateUnitOfMeasure(string name, string abbreviation, bool isBaseUnit)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var uom = new UnitOfMeasure { Name = name, Abbreviation = abbreviation, IsBaseUnit = isBaseUnit };
            _db.UnitsOfMeasure.Add(uom);
            await _db.SaveChangesAsync();
            return uom.Id;
        }

        public async Task DeleteUnitOfMeasure(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var uom = await _db.UnitsOfMeasure.FindAsync(id) ?? throw new KeyNotFoundException("UoM not found");
            uom.IsDeleted = true;
            await _db.SaveChangesAsync();
        }

        public async Task<List<UoMConversionDto>> GetConversions()
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var items = await _db.UoMConversions.Include(c => c.FromUoM).Include(c => c.ToUoM).ToListAsync();
            return _mapper.Map<List<UoMConversionDto>>(items);
        }

        public async Task<Guid> CreateConversion(Guid fromUoMId, Guid toUoMId, decimal conversionFactor)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var conversion = new UoMConversion { FromUoMId = fromUoMId, ToUoMId = toUoMId, ConversionFactor = conversionFactor };
            _db.UoMConversions.Add(conversion);
            await _db.SaveChangesAsync();
            return conversion.Id;
        }

        public async Task DeleteConversion(Guid id)
        {
            await using var _db = await _dbFactory.CreateDbContextAsync();

            var conversion = await _db.UoMConversions.FindAsync(id) ?? throw new KeyNotFoundException("Conversion not found");
            conversion.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
