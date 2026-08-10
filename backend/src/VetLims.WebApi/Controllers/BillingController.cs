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
public class BillingController : ControllerBase
{
    private readonly VetLimsDbContext _db;

    public BillingController(VetLimsDbContext db)
    {
        _db = db;
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<BillingInvoiceListDto>>> ListInvoices([FromQuery] BillingListQuery query, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(query.Page, query.PageSize, max: 200);

        var invoices = _db.BillingInvoices
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Animal)
            .Include(x => x.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = $"%{query.Search.Trim()}%";
            invoices = invoices.Where(x =>
                EF.Functions.ILike(x.InvoiceNumber, s) ||
                (x.Owner != null && EF.Functions.ILike(x.Owner.Name, s)) ||
                (x.Animal != null && EF.Functions.ILike(x.Animal.Name, s)));
        }

        if (query.Status.HasValue)
            invoices = invoices.Where(x => x.Status == query.Status.Value);

        if (query.ModuleType.HasValue)
            invoices = invoices.Where(x => x.Lines.Any(l => l.ModuleType == query.ModuleType.Value));

        var total = await invoices.CountAsync(ct);
        var items = await invoices
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new BillingInvoiceListDto(
                x.Id,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.Status,
                x.Owner != null ? x.Owner.Name : null,
                x.Animal != null ? x.Animal.Name : null,
                x.Currency,
                x.NetAmount,
                x.PaymentAmount,
                x.RefundAmount,
                x.CreditAmount,
                x.OutstandingAmount,
                x.CreditBalanceAmount
            ))
            .ToListAsync(ct);

        return Ok(new PagedResult<BillingInvoiceListDto>
        {
            Page = page,
            PageSize = size,
            Total = total,
            Items = items
        });
    }

    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<BillingInvoiceDetailDto>> GetInvoice(int id, CancellationToken ct)
    {
        var invoice = await LoadInvoiceGraph(id, ct);
        if (invoice is null)
            return NotFound();
        return Ok(MapInvoiceDetail(invoice));
    }

    [HttpPost("invoices")]
    public async Task<ActionResult<BillingInvoiceDetailDto>> CreateInvoice([FromBody] BillingInvoiceCreateDto dto, CancellationToken ct)
    {
        var invoice = new BillingInvoice();
        var validation = await ApplyInvoiceUpsertAsync(invoice, dto.InvoiceDate, dto.Status, dto.OwnerId, dto.AnimalId, dto.DoctorId, dto.RegistrationId, dto.AppointmentId, dto.Currency, dto.DiscountAmount, dto.Notes, dto.Lines, ct);
        if (validation is not null)
            return validation;

        _db.BillingInvoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        invoice.InvoiceNumber = $"INV-{invoice.Id:D8}";
        await _db.SaveChangesAsync(ct);

        var created = await LoadInvoiceGraph(invoice.Id, ct);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, MapInvoiceDetail(created!));
    }

    [HttpPut("invoices/{id:int}")]
    public async Task<ActionResult<BillingInvoiceDetailDto>> UpdateInvoice(int id, [FromBody] BillingInvoiceUpdateDto dto, CancellationToken ct)
    {
        var invoice = await _db.BillingInvoices
            .Include(x => x.Lines)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (invoice is null)
            return NotFound();

        var validation = await ApplyInvoiceUpsertAsync(invoice, dto.InvoiceDate, dto.Status, dto.OwnerId, dto.AnimalId, dto.DoctorId, dto.RegistrationId, dto.AppointmentId, dto.Currency, dto.DiscountAmount, dto.Notes, dto.Lines, ct);
        if (validation is not null)
            return validation;

        await _db.SaveChangesAsync(ct);

        var updated = await LoadInvoiceGraph(id, ct);
        return Ok(MapInvoiceDetail(updated!));
    }

    [HttpDelete("invoices/{id:int}")]
    public async Task<ActionResult> DeleteInvoice(int id, CancellationToken ct)
    {
        var invoice = await _db.BillingInvoices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (invoice is null)
            return NotFound();

        _db.BillingInvoices.Remove(invoice);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("invoices/{id:int}/transactions")]
    public async Task<ActionResult<BillingTransactionDto>> AddTransaction(int id, [FromBody] BillingTransactionCreateDto dto, CancellationToken ct)
    {
        if (dto.Amount <= 0)
            return BadRequest("Transaction amount must be greater than zero.");

        var invoice = await _db.BillingInvoices
            .Include(x => x.Lines)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (invoice is null)
            return NotFound();

        if (invoice.Status == BillingInvoiceStatus.Cancelled)
            return BadRequest("Cannot add transactions to a cancelled invoice.");

        var transaction = new BillingTransaction
        {
            BillingInvoiceId = id,
            TransactionType = dto.TransactionType,
            Method = dto.Method,
            Amount = decimal.Round(dto.Amount, 2),
            TransactedAt = dto.TransactedAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Reference = string.IsNullOrWhiteSpace(dto.Reference) ? null : dto.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };

        invoice.Transactions.Add(transaction);
        RecalculateTotals(invoice);

        await _db.SaveChangesAsync(ct);

        return Ok(new BillingTransactionDto(
            transaction.Id,
            transaction.TransactionType,
            transaction.Method,
            transaction.Amount,
            transaction.TransactedAt,
            transaction.Reference,
            transaction.Notes
        ));
    }

    [HttpGet("transactions/{transactionId:int}/receipt")]
    public async Task<ActionResult<BillingReceiptDto>> GetReceipt(int transactionId, CancellationToken ct)
    {
        var transaction = await _db.BillingTransactions
            .AsNoTracking()
            .Include(x => x.BillingInvoice)
                .ThenInclude(x => x.Owner)
            .Include(x => x.BillingInvoice)
                .ThenInclude(x => x.Animal)
            .FirstOrDefaultAsync(x => x.Id == transactionId && !x.IsDeleted, ct);

        if (transaction is null || transaction.BillingInvoice is null)
            return NotFound();

        var invoice = transaction.BillingInvoice;
        return Ok(new BillingReceiptDto(
            transaction.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            transaction.TransactedAt,
            transaction.TransactionType,
            transaction.Method,
            transaction.Amount,
            invoice.Currency,
            invoice.Owner?.Name,
            invoice.Animal?.Name,
            transaction.Reference,
            invoice.NetAmount,
            invoice.OutstandingAmount,
            invoice.CreditBalanceAmount
        ));
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<object>> GetLookups(CancellationToken ct)
    {
        var owners = await _db.Owners.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
        var animals = await _db.Animals.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.OwnerId, x.Code, x.Name }).ToListAsync(ct);
        var doctors = await _db.Doctors.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
        var registrations = await _db.Registrations.AsNoTracking().OrderByDescending(x => x.Id).Take(200).Select(x => new { x.Id, x.Barcode, x.OwnerName }).ToListAsync(ct);
        var appointments = await _db.Appointments.AsNoTracking().OrderByDescending(x => x.ScheduledAt).Take(200).Select(x => new { x.Id, x.ScheduledAt }).ToListAsync(ct);
        return Ok(new { owners, animals, doctors, registrations, appointments });
    }

    private async Task<ActionResult?> ApplyInvoiceUpsertAsync(
        BillingInvoice invoice,
        DateTime? invoiceDate,
        BillingInvoiceStatus status,
        int? ownerId,
        int? animalId,
        int? doctorId,
        int? registrationId,
        int? appointmentId,
        string currency,
        decimal discountAmount,
        string? notes,
        List<BillingInvoiceLineUpsertDto> lines,
        CancellationToken ct)
    {
        if (lines.Count == 0)
            return BadRequest("At least one billing line is required.");
        if (discountAmount < 0)
            return BadRequest("Discount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency))
            return BadRequest("Currency is required.");

        if (ownerId.HasValue && !await _db.Owners.AnyAsync(x => x.Id == ownerId.Value && x.IsActive, ct))
            return BadRequest("Selected owner was not found.");
        if (animalId.HasValue && !await _db.Animals.AnyAsync(x => x.Id == animalId.Value && x.IsActive, ct))
            return BadRequest("Selected animal was not found.");
        if (doctorId.HasValue && !await _db.Doctors.AnyAsync(x => x.Id == doctorId.Value && x.IsActive, ct))
            return BadRequest("Selected doctor was not found.");
        if (registrationId.HasValue && !await _db.Registrations.AnyAsync(x => x.Id == registrationId.Value, ct))
            return BadRequest("Selected registration was not found.");
        if (appointmentId.HasValue && !await _db.Appointments.AnyAsync(x => x.Id == appointmentId.Value, ct))
            return BadRequest("Selected appointment was not found.");

        invoice.InvoiceDate = invoiceDate?.ToUniversalTime() ?? DateTime.UtcNow;
        invoice.Status = status;
        invoice.OwnerId = ownerId;
        invoice.AnimalId = animalId;
        invoice.DoctorId = doctorId;
        invoice.RegistrationId = registrationId;
        invoice.AppointmentId = appointmentId;
        invoice.Currency = currency.Trim().ToUpperInvariant();
        invoice.DiscountAmount = decimal.Round(discountAmount, 2);
        invoice.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (invoice.Id == 0)
        {
            foreach (var lineDto in lines)
            {
                var line = CreateLine(lineDto);
                if (line is null)
                    return BadRequest("Each line requires description, quantity > 0, and non-negative pricing.");
                invoice.Lines.Add(line);
            }
        }
        else
        {
            _db.BillingInvoiceLines.RemoveRange(invoice.Lines);
            invoice.Lines.Clear();
            foreach (var lineDto in lines)
            {
                var line = CreateLine(lineDto);
                if (line is null)
                    return BadRequest("Each line requires description, quantity > 0, and non-negative pricing.");
                invoice.Lines.Add(line);
            }
        }

        RecalculateTotals(invoice);
        if (invoice.DiscountAmount > invoice.SubtotalAmount)
            return BadRequest("Invoice-level discount cannot exceed subtotal.");

        return null;
    }

    private static BillingInvoiceLine? CreateLine(BillingInvoiceLineUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Description) || dto.Quantity <= 0 || dto.UnitPrice < 0 || dto.DiscountAmount < 0)
            return null;

        var gross = decimal.Round(dto.Quantity * dto.UnitPrice, 2);
        if (dto.DiscountAmount > gross)
            return null;

        return new BillingInvoiceLine
        {
            ModuleType = dto.ModuleType,
            Description = dto.Description.Trim(),
            Quantity = decimal.Round(dto.Quantity, 3),
            UnitPrice = decimal.Round(dto.UnitPrice, 2),
            DiscountAmount = decimal.Round(dto.DiscountAmount, 2),
            TotalAmount = decimal.Round(gross - dto.DiscountAmount, 2)
        };
    }

    private static void RecalculateTotals(BillingInvoice invoice)
    {
        invoice.SubtotalAmount = decimal.Round(invoice.Lines.Sum(x => x.TotalAmount), 2);
        invoice.NetAmount = decimal.Round(Math.Max(invoice.SubtotalAmount - invoice.DiscountAmount, 0m), 2);

        invoice.PaymentAmount = decimal.Round(invoice.Transactions.Where(x => x.TransactionType == BillingTransactionType.Payment).Sum(x => x.Amount), 2);
        invoice.RefundAmount = decimal.Round(invoice.Transactions.Where(x => x.TransactionType == BillingTransactionType.Refund).Sum(x => x.Amount), 2);
        invoice.CreditAmount = decimal.Round(invoice.Transactions.Where(x => x.TransactionType == BillingTransactionType.Credit).Sum(x => x.Amount), 2);

        var settled = invoice.PaymentAmount + invoice.CreditAmount - invoice.RefundAmount;
        var outstanding = decimal.Round(invoice.NetAmount - settled, 2);

        invoice.OutstandingAmount = outstanding > 0 ? outstanding : 0m;
        invoice.CreditBalanceAmount = outstanding < 0 ? Math.Abs(outstanding) : 0m;
    }

    private async Task<BillingInvoice?> LoadInvoiceGraph(int id, CancellationToken ct)
    {
        return await _db.BillingInvoices
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Animal)
            .Include(x => x.Doctor)
            .Include(x => x.Registration)
            .Include(x => x.Lines)
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
    }

    private static BillingInvoiceDetailDto MapInvoiceDetail(BillingInvoice invoice)
    {
        return new BillingInvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.Status,
            invoice.OwnerId,
            invoice.Owner?.Name,
            invoice.AnimalId,
            invoice.Animal?.Name,
            invoice.DoctorId,
            invoice.Doctor?.Name,
            invoice.RegistrationId,
            invoice.Registration?.Barcode,
            invoice.AppointmentId,
            invoice.Currency,
            invoice.Notes,
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.NetAmount,
            invoice.PaymentAmount,
            invoice.RefundAmount,
            invoice.CreditAmount,
            invoice.OutstandingAmount,
            invoice.CreditBalanceAmount,
            invoice.Lines.OrderBy(x => x.Id).Select(x => new BillingInvoiceLineDto(
                x.Id,
                x.ModuleType,
                x.Description,
                x.Quantity,
                x.UnitPrice,
                x.DiscountAmount,
                x.TotalAmount
            )),
            invoice.Transactions.OrderByDescending(x => x.TransactedAt).Select(x => new BillingTransactionDto(
                x.Id,
                x.TransactionType,
                x.Method,
                x.Amount,
                x.TransactedAt,
                x.Reference,
                x.Notes
            ))
        );
    }
}
