using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.WebApi.Common;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryPharmacyController : ControllerBase
{
    private readonly VetLimsDbContext _db;

    public InventoryPharmacyController(VetLimsDbContext db)
    {
        _db = db;
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<PagedResult<InventoryPurchaseListDto>>> ListPurchases([FromQuery] InventoryPurchaseListQuery query, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(query.Page, query.PageSize, max: 200);
        var purchases = _db.InventoryPurchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            purchases = purchases.Where(x =>
                EF.Functions.ILike(x.PurchaseNumber, s) ||
                (x.InvoiceReference != null && EF.Functions.ILike(x.InvoiceReference, s)) ||
                EF.Functions.ILike(x.Supplier.Name, s));
        }
        if (query.SupplierId.HasValue)
            purchases = purchases.Where(x => x.SupplierId == query.SupplierId.Value);

        var total = await purchases.CountAsync(ct);
        var items = await purchases
            .OrderByDescending(x => x.PurchaseDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new InventoryPurchaseListDto(
                x.Id,
                x.PurchaseNumber,
                x.PurchaseDate,
                x.SupplierId,
                x.Supplier.Name,
                x.Currency,
                x.TotalAmount))
            .ToListAsync(ct);

        return Ok(new PagedResult<InventoryPurchaseListDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpGet("purchases/{id:int}")]
    public async Task<ActionResult<InventoryPurchaseDetailDto>> GetPurchase(int id, CancellationToken ct)
    {
        var purchase = await LoadPurchaseGraph(id, ct);
        if (purchase is null) return NotFound();
        return Ok(MapPurchaseDetail(purchase));
    }

    [HttpPost("purchases")]
    public async Task<ActionResult<InventoryPurchaseDetailDto>> CreatePurchase([FromBody] InventoryPurchaseCreateDto dto, CancellationToken ct)
    {
        if (dto.Lines.Count == 0)
            return BadRequest("At least one purchase line is required.");
        if (string.IsNullOrWhiteSpace(dto.Currency))
            return BadRequest("Currency is required.");

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId && x.IsActive && !x.IsDeleted, ct);
        if (supplier is null)
            return BadRequest("Selected supplier was not found.");

        var purchase = new InventoryPurchase
        {
            SupplierId = dto.SupplierId,
            PurchaseDate = dto.PurchaseDate?.ToUniversalTime() ?? DateTime.UtcNow,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            InvoiceReference = Clean(dto.InvoiceReference),
            Notes = Clean(dto.Notes)
        };

        foreach (var line in dto.Lines)
        {
            var validation = await ValidateLineAsync(line, ct);
            if (validation is not null) return validation;
            purchase.Lines.Add(CreatePurchaseLine(line));
        }

        RecalculatePurchase(purchase);
        _db.InventoryPurchases.Add(purchase);
        await _db.SaveChangesAsync(ct);
        purchase.PurchaseNumber = $"PUR-{purchase.Id:D8}";
        await _db.SaveChangesAsync(ct);

        var created = await LoadPurchaseGraph(purchase.Id, ct);
        return CreatedAtAction(nameof(GetPurchase), new { id = purchase.Id }, MapPurchaseDetail(created!));
    }

    [HttpDelete("purchases/{id:int}")]
    public async Task<ActionResult> DeletePurchase(int id, CancellationToken ct)
    {
        var purchase = await _db.InventoryPurchases
            .Include(x => x.Lines)
            .ThenInclude(x => x.ReturnLines)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Consumptions)
            .Include(x => x.Returns)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (purchase is null)
            return NotFound();

        _db.InventoryPurchases.Remove(purchase);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("purchases/{purchaseId:int}/returns")]
    public async Task<ActionResult<InventoryPurchaseReturnDto>> CreatePurchaseReturn(int purchaseId, [FromBody] InventoryPurchaseReturnCreateDto dto, CancellationToken ct)
    {
        if (dto.Lines.Count == 0)
            return BadRequest("At least one return line is required.");

        var purchase = await _db.InventoryPurchases
            .Include(x => x.Lines)
            .Include(x => x.Returns)
            .ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == purchaseId && !x.IsDeleted, ct);
        if (purchase is null)
            return NotFound();

        var lineMap = purchase.Lines.ToDictionary(x => x.Id);
        var ret = new InventoryPurchaseReturn
        {
            InventoryPurchaseId = purchaseId,
            ReturnDate = dto.ReturnDate?.ToUniversalTime() ?? DateTime.UtcNow,
            Notes = Clean(dto.Notes)
        };

        foreach (var item in dto.Lines)
        {
            if (item.Quantity <= 0)
                return BadRequest("Return quantity must be greater than zero.");
            if (!lineMap.TryGetValue(item.PurchaseLineId, out var line))
                return BadRequest($"Purchase line not found: {item.PurchaseLineId}");
            if (item.Quantity > line.AvailableQuantity)
                return BadRequest($"Return quantity exceeds available stock for batch {line.BatchNumber}.");

            var qty = decimal.Round(item.Quantity, 3);
            var amount = decimal.Round(qty * line.UnitCost, 2);
            ret.Lines.Add(new InventoryPurchaseReturnLine
            {
                InventoryPurchaseLineId = line.Id,
                Quantity = qty,
                UnitCost = line.UnitCost,
                ReturnAmount = amount,
                Reason = Clean(item.Reason)
            });

            line.ReturnedQuantity = decimal.Round(line.ReturnedQuantity + qty, 3);
            line.AvailableQuantity = decimal.Round(line.AvailableQuantity - qty, 3);
        }

        ret.TotalReturnAmount = decimal.Round(ret.Lines.Sum(x => x.ReturnAmount), 2);
        ret.ReturnNumber = $"PRT-{purchaseId:D6}-{purchase.Returns.Count + 1:D3}";
        purchase.Returns.Add(ret);
        RecalculatePurchase(purchase);

        await _db.SaveChangesAsync(ct);

        return Ok(new InventoryPurchaseReturnDto(
            ret.Id,
            ret.ReturnNumber,
            ret.ReturnDate,
            ret.TotalReturnAmount,
            ret.Notes,
            ret.Lines.Select(x => new InventoryPurchaseReturnLineDto(
                x.Id,
                x.InventoryPurchaseLineId,
                x.Quantity,
                x.UnitCost,
                x.ReturnAmount,
                x.Reason
            ))));
    }

    [HttpPost("stock/{purchaseLineId:int}/consume")]
    public async Task<ActionResult<InventoryConsumptionDto>> ConsumeStock(int purchaseLineId, [FromBody] InventoryConsumptionCreateDto dto, CancellationToken ct)
    {
        if (dto.Quantity <= 0)
            return BadRequest("Consumption quantity must be greater than zero.");

        var line = await _db.InventoryPurchaseLines
            .Include(x => x.InventoryPurchase)
            .FirstOrDefaultAsync(x => x.Id == purchaseLineId && !x.IsDeleted, ct);
        if (line is null)
            return NotFound();
        if (dto.Quantity > line.AvailableQuantity)
            return BadRequest("Consumption quantity exceeds available stock.");

        var qty = decimal.Round(dto.Quantity, 3);
        var consumption = new InventoryConsumption
        {
            InventoryPurchaseLineId = purchaseLineId,
            Quantity = qty,
            ConsumedAt = dto.ConsumedAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Notes = Clean(dto.Notes)
        };

        line.ConsumedQuantity = decimal.Round(line.ConsumedQuantity + qty, 3);
        line.AvailableQuantity = decimal.Round(line.AvailableQuantity - qty, 3);

        _db.InventoryConsumptions.Add(consumption);

        var purchase = await _db.InventoryPurchases
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == line.InventoryPurchaseId, ct);
        RecalculatePurchase(purchase);

        await _db.SaveChangesAsync(ct);

        return Ok(new InventoryConsumptionDto(
            consumption.Id,
            consumption.InventoryPurchaseLineId,
            consumption.Quantity,
            consumption.ConsumedAt,
            consumption.Notes));
    }

    [HttpGet("stock")]
    public async Task<ActionResult<PagedResult<InventoryStockBatchDto>>> ListStock([FromQuery] InventoryStockListQuery query, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(query.Page, query.PageSize, max: 200);

        var stock = _db.InventoryPurchaseLines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Include(x => x.InventoryPurchase)
            .Where(x => !x.IsDeleted && !x.InventoryPurchase.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            stock = stock.Where(x =>
                EF.Functions.ILike(x.BatchNumber, s) ||
                EF.Functions.ILike(x.Medicine.Name, s) ||
                EF.Functions.ILike(x.InventoryPurchase.PurchaseNumber, s));
        }

        if (query.ExpiryWithinDays.HasValue && query.ExpiryWithinDays.Value >= 0)
        {
            var until = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(query.ExpiryWithinDays.Value));
            stock = stock.Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value <= until);
        }

        if (query.LowStockOnly)
            stock = stock.Where(x => x.AvailableQuantity <= x.LowStockThreshold);

        var total = await stock.CountAsync(ct);
        var items = await stock
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.Medicine.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new InventoryStockBatchDto(
                x.Id,
                x.InventoryPurchaseId,
                x.InventoryPurchase.PurchaseNumber,
                x.InventoryPurchase.PurchaseDate,
                x.MedicineId,
                x.Medicine.Name,
                x.BatchNumber,
                x.ExpiryDate,
                x.Quantity,
                x.ReturnedQuantity,
                x.ConsumedQuantity,
                x.AvailableQuantity,
                x.LowStockThreshold,
                x.AvailableQuantity <= x.LowStockThreshold,
                x.ExpiryDate.HasValue && x.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date)
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<InventoryStockBatchDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpGet("stock/alerts/low")]
    public async Task<ActionResult<IEnumerable<InventoryStockBatchDto>>> LowStockAlerts(CancellationToken ct)
    {
        var alerts = await _db.InventoryPurchaseLines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Include(x => x.InventoryPurchase)
            .Where(x => !x.IsDeleted && !x.InventoryPurchase.IsDeleted && x.AvailableQuantity <= x.LowStockThreshold)
            .OrderBy(x => x.AvailableQuantity)
            .Select(x => new InventoryStockBatchDto(
                x.Id,
                x.InventoryPurchaseId,
                x.InventoryPurchase.PurchaseNumber,
                x.InventoryPurchase.PurchaseDate,
                x.MedicineId,
                x.Medicine.Name,
                x.BatchNumber,
                x.ExpiryDate,
                x.Quantity,
                x.ReturnedQuantity,
                x.ConsumedQuantity,
                x.AvailableQuantity,
                x.LowStockThreshold,
                true,
                x.ExpiryDate.HasValue && x.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date)
            ))
            .Take(200)
            .ToListAsync(ct);

        return Ok(alerts);
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<object>> Lookups(CancellationToken ct)
    {
        var suppliers = await _db.Suppliers.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
        var medicines = await _db.Medicines.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
        var inventoryCategories = await _db.InventoryCategories.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
        return Ok(new { suppliers, medicines, inventoryCategories });
    }

    private async Task<ActionResult?> ValidateLineAsync(InventoryPurchaseLineUpsertDto line, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line.BatchNumber))
            return BadRequest("Batch number is required.");
        if (line.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");
        if (line.UnitCost < 0)
            return BadRequest("Unit cost cannot be negative.");
        if (line.LowStockThreshold < 0)
            return BadRequest("Low-stock threshold cannot be negative.");

        var medicineExists = await _db.Medicines.AnyAsync(x => x.Id == line.MedicineId && x.IsActive && !x.IsDeleted, ct);
        if (!medicineExists)
            return BadRequest($"Medicine not found: {line.MedicineId}");
        if (line.InventoryCategoryId.HasValue)
        {
            var categoryExists = await _db.InventoryCategories.AnyAsync(x => x.Id == line.InventoryCategoryId.Value && x.IsActive && !x.IsDeleted, ct);
            if (!categoryExists)
                return BadRequest($"Inventory category not found: {line.InventoryCategoryId.Value}");
        }
        return null;
    }

    private static InventoryPurchaseLine CreatePurchaseLine(InventoryPurchaseLineUpsertDto dto)
    {
        var quantity = decimal.Round(dto.Quantity, 3);
        var unitCost = decimal.Round(dto.UnitCost, 2);
        return new InventoryPurchaseLine
        {
            MedicineId = dto.MedicineId,
            InventoryCategoryId = dto.InventoryCategoryId,
            BatchNumber = dto.BatchNumber.Trim(),
            ExpiryDate = dto.ExpiryDate,
            Quantity = quantity,
            UnitCost = unitCost,
            LowStockThreshold = decimal.Round(dto.LowStockThreshold, 3),
            ReturnedQuantity = 0m,
            ConsumedQuantity = 0m,
            AvailableQuantity = quantity,
            LineAmount = decimal.Round(quantity * unitCost, 2)
        };
    }

    private static void RecalculatePurchase(InventoryPurchase purchase)
    {
        purchase.TotalAmount = decimal.Round(purchase.Lines.Sum(x => x.LineAmount) - purchase.Returns.Sum(x => x.TotalReturnAmount), 2);
    }

    private async Task<InventoryPurchase?> LoadPurchaseGraph(int id, CancellationToken ct)
    {
        return await _db.InventoryPurchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(x => x.Medicine)
            .Include(x => x.Lines).ThenInclude(x => x.InventoryCategory)
            .Include(x => x.Returns).ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    private static InventoryPurchaseDetailDto MapPurchaseDetail(InventoryPurchase purchase)
    {
        return new InventoryPurchaseDetailDto(
            purchase.Id,
            purchase.PurchaseNumber,
            purchase.PurchaseDate,
            purchase.SupplierId,
            purchase.Supplier.Name,
            purchase.Currency,
            purchase.InvoiceReference,
            purchase.Notes,
            purchase.TotalAmount,
            purchase.Lines.OrderBy(x => x.Id).Select(x => new InventoryPurchaseLineDto(
                x.Id,
                x.MedicineId,
                x.Medicine.Name,
                x.InventoryCategoryId,
                x.InventoryCategory?.Name,
                x.BatchNumber,
                x.ExpiryDate,
                x.Quantity,
                x.UnitCost,
                x.LowStockThreshold,
                x.ReturnedQuantity,
                x.ConsumedQuantity,
                x.AvailableQuantity,
                x.LineAmount
            )),
            purchase.Returns.OrderByDescending(x => x.ReturnDate).Select(r => new InventoryPurchaseReturnDto(
                r.Id,
                r.ReturnNumber,
                r.ReturnDate,
                r.TotalReturnAmount,
                r.Notes,
                r.Lines.Select(x => new InventoryPurchaseReturnLineDto(
                    x.Id,
                    x.InventoryPurchaseLineId,
                    x.Quantity,
                    x.UnitCost,
                    x.ReturnAmount,
                    x.Reason
                ))
            ))
        );
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
