using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class KitService : IKitService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public KitService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<KitDto>> GetKits()
    {
        var kits = await _context.Kits
            .Include(k => k.Product)
            .Include(k => k.Components).ThenInclude(c => c.ComponentProduct)
            .ToListAsync();
        return _mapper.Map<List<KitDto>>(kits);
    }

    public async Task<KitDto> GetKitById(Guid id)
    {
        var kit = await _context.Kits
            .Include(k => k.Product)
            .Include(k => k.Components).ThenInclude(c => c.ComponentProduct)
            .FirstOrDefaultAsync(k => k.Id == id)
            ?? throw new EntityNotFoundException("Kit", id);
        return _mapper.Map<KitDto>(kit);
    }

    public async Task<Guid> CreateKit(CreateKitDto dto)
    {
        var kit = new Kit
        {
            ProductId = dto.ProductId,
            IsActive = true
        };

        foreach (var c in dto.Components)
        {
            kit.Components.Add(new KitComponent
            {
                ComponentProductId = c.ComponentProductId,
                Quantity = c.Quantity
            });
        }

        _context.Kits.Add(kit);
        await _context.SaveChangesAsync();
        return kit.Id;
    }

    public async Task AssembleKit(KitAssemblyDto dto)
    {
        var kit = await _context.Kits
            .Include(k => k.Components)
            .FirstOrDefaultAsync(k => k.Id == dto.KitId)
            ?? throw new EntityNotFoundException("Kit", dto.KitId);

        foreach (var comp in kit.Components)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == comp.ComponentProductId && i.WarehouseId == dto.WarehouseId)
                ?? throw new DomainException($"No inventory for component {comp.ComponentProductId} in warehouse.");

            var required = comp.Quantity * dto.Quantity;
            if (inv.Quantity < required)
                throw new DomainException($"Insufficient stock for component. Need {required}, have {inv.Quantity}.");

            inv.Quantity -= required;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = comp.ComponentProductId,
                WarehouseId = dto.WarehouseId,
                Type = StockMovementType.KitAssembly,
                Quantity = -required,
                MovementDate = DateTime.UtcNow,
                Reference = $"Kit-{kit.Id.ToString()[..8]}"
            });
        }

        var kitInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == kit.ProductId && i.WarehouseId == dto.WarehouseId);

        if (kitInv == null)
        {
            kitInv = new Inventory { ProductId = kit.ProductId, WarehouseId = dto.WarehouseId, Quantity = 0 };
            _context.Inventories.Add(kitInv);
        }

        kitInv.Quantity += dto.Quantity;
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = kit.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = StockMovementType.KitAssembly,
            Quantity = dto.Quantity,
            MovementDate = DateTime.UtcNow,
            Reference = $"Kit-{kit.Id.ToString()[..8]}"
        });

        await _context.SaveChangesAsync();
    }

    public async Task DisassembleKit(KitAssemblyDto dto)
    {
        var kit = await _context.Kits
            .Include(k => k.Components)
            .FirstOrDefaultAsync(k => k.Id == dto.KitId)
            ?? throw new EntityNotFoundException("Kit", dto.KitId);

        var kitInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == kit.ProductId && i.WarehouseId == dto.WarehouseId)
            ?? throw new DomainException("No kit inventory to disassemble.");

        if (kitInv.Quantity < dto.Quantity)
            throw new DomainException($"Insufficient kit stock. Have {kitInv.Quantity}, need {dto.Quantity}.");

        kitInv.Quantity -= dto.Quantity;
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = kit.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = StockMovementType.KitDisassembly,
            Quantity = -dto.Quantity,
            MovementDate = DateTime.UtcNow,
            Reference = $"Kit-{kit.Id.ToString()[..8]}"
        });

        foreach (var comp in kit.Components)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == comp.ComponentProductId && i.WarehouseId == dto.WarehouseId);

            if (inv == null)
            {
                inv = new Inventory { ProductId = comp.ComponentProductId, WarehouseId = dto.WarehouseId, Quantity = 0 };
                _context.Inventories.Add(inv);
            }

            var returned = comp.Quantity * dto.Quantity;
            inv.Quantity += returned;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = comp.ComponentProductId,
                WarehouseId = dto.WarehouseId,
                Type = StockMovementType.KitDisassembly,
                Quantity = returned,
                MovementDate = DateTime.UtcNow,
                Reference = $"Kit-{kit.Id.ToString()[..8]}"
            });
        }

        await _context.SaveChangesAsync();
    }
}
