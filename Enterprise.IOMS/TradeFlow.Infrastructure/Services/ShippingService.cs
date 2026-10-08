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

// ─── Shipping Service ───────────────────────────────────────────────────────
public class ShippingService : IShippingService
{
    private readonly ApplicationDbContext _context;

    public ShippingService(ApplicationDbContext context) => _context = context;

    public async Task<Guid> CreateDeliveryNote(CreateDeliveryNoteDto dto)
    {
        var order = await _context.SalesOrders.FindAsync(dto.SalesOrderId)
            ?? throw new EntityNotFoundException("SalesOrder", dto.SalesOrderId);

        var dn = new DeliveryNote
        {
            DeliveryNoteNumber = NumberGenerator.GenerateDeliveryNoteNumber(),
            SalesOrderId = dto.SalesOrderId,
            Date = dto.ShippedDate ?? DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.DeliveryNotes.Add(dn);
        await _context.SaveChangesAsync();
        return dn.Id;
    }

    public async Task<Guid> CreateShipment(CreateShipmentDto dto)
    {
        var shipment = new Shipment
        {
            SalesOrderId = dto.DeliveryNoteId, // maps from UI's DeliveryNoteId context to SalesOrderId
            CarrierName = dto.Carrier,
            TrackingNumber = dto.TrackingNumber,
            ShipDate = dto.ShippedDate ?? DateTime.UtcNow,
            Status = ShipmentStatus.Pending
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();
        return shipment.Id;
    }

    public async Task UpdateShipmentStatus(Guid id, ShipmentStatus status)
    {
        var shipment = await _context.Shipments.FindAsync(id)
            ?? throw new EntityNotFoundException("Shipment", id);

        shipment.Status = status;
        if (status == ShipmentStatus.Delivered)
            shipment.ActualDelivery = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ShipmentDto>> GetShipments(string? search, int page, int pageSize)
    {
        var query = _context.Shipments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => (s.TrackingNumber != null && s.TrackingNumber.Contains(search))
                || (s.CarrierName != null && s.CarrierName.Contains(search)));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.ShipDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ShipmentDto(
                s.Id, null, s.CarrierName, s.TrackingNumber,
                s.ShipDate, s.ActualDelivery, s.Status))
            .ToListAsync();


        //var statsQuery = _context.Shipments.IgnoreQueryFilters().AsNoTracking().Where(t => !t.IsDeleted).AsQueryable();
        //if (request.TenantId.HasValue && request.TenantId.Value != 0)
        //    statsQuery = statsQuery.Where(t => t.TenantId == request.TenantId.Value);
        //var statusGroups = await statsQuery.GroupBy(t => t.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync();
        //var stats = statusGroups.ToDictionary(x => x.Status, x => x.Count);
        //stats["All"] = await statsQuery.CountAsync();


       
        return new PagedResult<ShipmentDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<DeliveryNoteDto>> GetDeliveryNotesPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.DeliveryNotes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.DeliveryNoteNumber.Contains(search)
                || d.SalesOrder.OrderNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DeliveryNoteDto(d.Id, d.DeliveryNoteNumber, d.SalesOrderId,
                d.SalesOrder.OrderNumber, d.Date, d.ShippedBy, d.TrackingNumber))
            .ToListAsync();

        return new PagedResult<DeliveryNoteDto>(items, total, page, pageSize);
    }
}
