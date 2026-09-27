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
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UoMService(ApplicationDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

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
}
