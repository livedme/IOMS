using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace TradeFlow.Infrastructure.Services
{
    public class ProductSerialService : IProductSerialService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ProductSerialService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductSerialDto>> GetSerials(Guid? productId, ProductSerialStatus? status, int page, int pageSize)
        {
            var query = _db.ProductSerials
                .Include(ps => ps.Product)
                .Include(ps => ps.Warehouse)
                .Include(ps => ps.Supplier)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(ps => ps.ProductId == productId.Value);
            if (status.HasValue)
                query = query.Where(ps => ps.Status == status.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(ps => ps.Product.Name).ThenBy(ps => ps.SerialNumber)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductSerialDto>(_mapper.Map<List<ProductSerialDto>>(items), total, page, pageSize);
        }

        public async Task<ProductSerialDto?> GetSerialById(Guid id)
        {
            var serial = await _db.ProductSerials
                .Include(ps => ps.Product)
                .Include(ps => ps.Warehouse)
                .Include(ps => ps.Supplier)
                .FirstOrDefaultAsync(ps => ps.Id == id);
            return serial == null ? null : _mapper.Map<ProductSerialDto>(serial);
        }

        public async Task<List<ProductSerialDto>> GetAvailableSerialsByProduct(Guid productId)
        {
            var serials = await _db.ProductSerials
                .Include(ps => ps.Product)
                .Include(ps => ps.Warehouse)
                .Include(ps => ps.Supplier)
                .Where(ps => ps.ProductId == productId && ps.Status == ProductSerialStatus.Available)
                .OrderBy(ps => ps.SerialNumber)
                .ToListAsync();
            return _mapper.Map<List<ProductSerialDto>>(serials);
        }

        public async Task<Guid> CreateSerial(CreateProductSerialDto dto)
        {
            var serial = new ProductSerial
            {
                ProductId = dto.ProductId,
                SerialNumber = dto.SerialNumber,
                Barcode = dto.Barcode,
                QRCode = dto.QRCode,
                PurchaseDate = dto.PurchaseDate,
                WarrantyStartDate = dto.WarrantyStartDate,
                WarrantyEndDate = dto.WarrantyEndDate,
                Status = ProductSerialStatus.Available,
                WarehouseId = dto.WarehouseId,
                BinLocation = dto.BinLocation,
                SupplierId = dto.SupplierId
            };
            _db.ProductSerials.Add(serial);
            await _db.SaveChangesAsync();
            return serial.Id;
        }

        public async Task<List<Guid>> CreateSerials(List<CreateProductSerialDto> dtos)
        {
            var serials = dtos.Select(dto => new ProductSerial
            {
                ProductId = dto.ProductId,
                SerialNumber = dto.SerialNumber,
                Barcode = dto.Barcode,
                QRCode = dto.QRCode,
                PurchaseDate = dto.PurchaseDate,
                WarrantyStartDate = dto.WarrantyStartDate,
                WarrantyEndDate = dto.WarrantyEndDate,
                Status = ProductSerialStatus.Available,
                WarehouseId = dto.WarehouseId,
                BinLocation = dto.BinLocation,
                SupplierId = dto.SupplierId
            }).ToList();

            _db.ProductSerials.AddRange(serials);
            await _db.SaveChangesAsync();
            return serials.Select(s => s.Id).ToList();
        }

        public async Task UpdateStatus(Guid id, ProductSerialStatus status)
        {
            var serial = await _db.ProductSerials.FindAsync(id)
                ?? throw new KeyNotFoundException("Product serial not found");
            serial.Status = status;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSerial(Guid id)
        {
            var serial = await _db.ProductSerials.FindAsync(id)
                ?? throw new KeyNotFoundException("Product serial not found");
            serial.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
