using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Common;

namespace VetLims.WebApi.Controllers;

[Authorize] // restrict further via policy/roles if needed
[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    private readonly PricingService _pricing;
    private readonly BarcodeService _barcodes;
    private readonly ICurrentUserService _currentUserService;

    public RegistrationsController(VetLimsDbContext db, PricingService pricing, BarcodeService barcodes, ICurrentUserService currentUserService)
    {
        _db = db; _pricing = pricing; _barcodes = barcodes; _currentUserService = currentUserService;
    }

    // List + filters (owner search, client, species)
    [HttpGet]
    public async Task<ActionResult<PagedResult<RegistrationDto>>> List([FromQuery] ListQuery q)
    {
        var page = Math.Max(q.Page, 1);
        var size = Math.Clamp(q.PageSize, 1, 200);

        var query = _db.Registrations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.OwnerName, s) ||
                EF.Functions.ILike(x.Barcode, s) ||
                (x.ReferenceNumber != null && EF.Functions.ILike(x.ReferenceNumber, s)) ||
                (x.ClinicalNotes != null && EF.Functions.ILike(x.ClinicalNotes, s)));
        }
        if (q.ClientId.HasValue) query = query.Where(x => x.ClientId == q.ClientId.Value);
        if (q.SpeciesId.HasValue)
        {
            query = query.Where(reg => reg.Animals.Any(a => a.SpeciesId == q.SpeciesId.Value));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new RegistrationDto(
                x.Id,
                x.OwnerName,
                x.ClientId,
                x.OwnerId,
                x.DoctorId,
                x.Barcode,
                x.ReferenceNumber,
                x.ClinicalNotes,
                x.CreatedAt,
                x.TotalAmount,
                x.TotalVat,
                x.PaidAmount,
                x.Balance,
                x.OwnerMobile))
            .ToListAsync();

        return Ok(new PagedResult<RegistrationDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    // Create registration: animals + items + optional initial payment
    [HttpPost]
    public async Task<ActionResult<RegistrationDetailDto>> Create([FromBody] RegistrationCreateDto dto)
    {
        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue)
            return Forbid();

        var branchId = dto.BranchId ?? _currentUserService.BranchId;
        if (!branchId.HasValue)
            return BadRequest("Branch selection is required.");

        var branch = await _db.Branches.Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == branchId.Value && x.Clinic.OrganizationId == organizationId.Value && !x.IsDeleted && x.IsActive);
        if (branch is null)
            return BadRequest("Selected branch is not available for your organization.");

        if (dto.Animals.Count == 0)
            return BadRequest("At least one animal is required.");
        if (dto.Items.Count == 0)
            return BadRequest("At least one test order is required.");

        Owner? owner = null;
        if (dto.OwnerId.HasValue)
        {
            owner = await _db.Owners.FirstOrDefaultAsync(x => x.Id == dto.OwnerId.Value && x.IsActive);
            if (owner is null) return BadRequest("Selected owner was not found.");
        }

        if (dto.DoctorId.HasValue)
        {
            var doctorExists = await _db.Doctors.AnyAsync(x => x.Id == dto.DoctorId.Value && x.IsActive);
            if (!doctorExists) return BadRequest("Selected doctor was not found.");
        }

        var ownerName = owner?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(ownerName))
            ownerName = dto.OwnerName.Trim();
        if (string.IsNullOrWhiteSpace(ownerName))
            return BadRequest("Owner name is required.");

        var ownerMobile = owner?.Mobile ?? dto.OwnerMobile?.Trim();

        var reg = new Registration
        {
           ClinicId = branch.ClinicId,
           BranchId = branch.Id,
           OwnerName = ownerName,
           OwnerMobile = ownerMobile,
           ClientId = dto.ClientId,
           OwnerId = dto.OwnerId,
           DoctorId = dto.DoctorId,
           ReferenceNumber = dto.ReferenceNumber?.Trim(),
           ClinicalNotes = dto.ClinicalNotes?.Trim(),
           Barcode = await _barcodes.NextAsync()
        };

        // animals
        foreach (var a in dto.Animals)
        {
            Animal? animal = null;
            if (a.AnimalId.HasValue)
            {
                animal = await _db.Animals.FirstOrDefaultAsync(x => x.Id == a.AnimalId.Value);
                if (animal is null) return BadRequest($"Selected animal was not found: {a.AnimalId.Value}");
                if (dto.OwnerId.HasValue && animal.OwnerId != dto.OwnerId.Value)
                    return BadRequest("Selected animal does not belong to the selected owner.");
            }

            var speciesId = animal?.SpeciesId ?? a.SpeciesId;
            var animalName = animal?.Name?.Trim() ?? a.AnimalName?.Trim();
            if (string.IsNullOrWhiteSpace(animalName))
                return BadRequest("Animal name is required.");

            reg.Animals.Add(new RegistrationAnimal
            {
                AnimalId = a.AnimalId,
                SpeciesId = speciesId,
                AnimalName = animalName,
                AgeInDays = a.AgeInDays ?? (animal?.DateOfBirth.HasValue == true
                    ? Math.Max((DateTime.UtcNow.Date - animal.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue).Date).Days, 0)
                    : null)
            });
        }

        // items with pricing + per-item barcode
        // Quantity = number of animals this order applies to:
        //   item.SpeciesId set   -> count of animals with that species
        //   item.SpeciesId null   -> all animals on the registration
        decimal totalNet = 0, totalVat = 0;
        foreach (var it in dto.Items)
        {
            if (it.SampleTypeId.HasValue)
            {
                var sampleTypeExists = await _db.SampleTypes.AnyAsync(x => x.Id == it.SampleTypeId.Value && x.IsActive);
                if (!sampleTypeExists) return BadRequest($"Selected sample type was not found: {it.SampleTypeId.Value}");
            }

            if (it.ItemType == RegItemType.Test)
            {
                var (isAllowed, error) = await TestBranchAvailabilityService.ValidateTestUseAsync(_db, it.ItemId, branch.Id, organizationId.Value);
                if (!isAllowed)
                    return BadRequest(error ?? "The selected Test is not enabled for this Branch.");
            }

            var quantity = it.SpeciesId.HasValue
                ? reg.Animals.Count(a => a.SpeciesId == it.SpeciesId.Value)
                : reg.Animals.Count;

            var (price, vatRate) = await _pricing.ResolvePricingAsync(dto.ClientId, it.ItemType, it.ItemId);
            var unitVat = price > 0 ? decimal.Round(price * vatRate / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
            var item = new RegistrationItem
            {
                ClinicId = branch.ClinicId,
                BranchId = branch.Id,
                ItemType = it.ItemType,
                ItemId = it.ItemId,
                SpeciesId = it.SpeciesId,
                SampleTypeId = it.SampleTypeId,
                Price = price,
                Quantity = quantity,
                VatRate = vatRate,
                VatAmount = unitVat * quantity,
                ItemBarcode = await _barcodes.NextAsync("IT")
            };
            reg.Items.Add(item);
            totalNet += price * quantity;
            totalVat += unitVat * quantity;
        }
        reg.TotalAmount = decimal.Round(totalNet + totalVat, 2, MidpointRounding.AwayFromZero);
        reg.TotalVat = decimal.Round(totalVat, 2, MidpointRounding.AwayFromZero);

        // optional initial payment
        if (dto.InitialPaymentAmount.HasValue && dto.InitialPaymentAmount.Value > 0 && dto.InitialPaymentMethod.HasValue)
        {
            var p = new Payment
            {
                Method = dto.InitialPaymentMethod.Value,
                Amount = dto.InitialPaymentAmount.Value,
                Reference = dto.InitialPaymentRef,
                Currency = "AED"
            };
            reg.Payments.Add(p);
            reg.PaidAmount += p.Amount;
        }

        _db.Registrations.Add(reg);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = reg.Id }, await ProjectDetail(reg.Id));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegistrationDetailDto>> Get(int id)
    {
        var d = await ProjectDetail(id);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<object>> GetLookups()
    {
        var owners = await _db.Owners.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, OwnerCode = x.Code, x.Name, x.Mobile })
            .ToListAsync();

        var animals = await _db.Animals.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.OwnerId, AnimalCode = x.Code, x.Name, x.SpeciesId })
            .ToListAsync();

        var doctors = await _db.Doctors.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var sampleTypes = await _db.SampleTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var species = await _db.Species.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var clients = await _db.Clients.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync();

        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue)
            return Forbid();

        // Only offer tests that are enabled for the logged-in branch (WYSIWYG with create-time validation)
        var branchId = _currentUserService.BranchId;
        List<int> enabledTestIds = new();
        if (branchId.HasValue)
        {
            enabledTestIds = await (from bt in _db.BranchTests.AsNoTracking()
                                    join bd in _db.BranchDepartments.AsNoTracking()
                                        on new { bt.BranchId, bt.Test.DepartmentId } equals new { bd.BranchId, bd.DepartmentId }
                                    where bt.BranchId == branchId.Value && !bt.IsDeleted && bt.IsActive
                                          && !bd.IsDeleted && bd.IsActive
                                    select bt.TestId).ToListAsync();
        }

        var tests = await _db.Tests.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value && !x.IsDeleted && x.IsActive
                && (branchId.HasValue ? enabledTestIds.Contains(x.Id) : true))
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, ItemType = RegItemType.Test, x.DepartmentId, SampleTypeId = (int?)null })
            .ToListAsync();

        var profiles = await _db.Profiles.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, ItemType = RegItemType.Profile, DepartmentId = (int?)null, SampleTypeId = (int?)null })
            .ToListAsync();

        var packages = await _db.Packages.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, ItemType = RegItemType.Package, DepartmentId = (int?)null, SampleTypeId = (int?)null })
            .ToListAsync();

        return Ok(new
        {
            owners,
            animals,
            doctors,
            clients,
            species,
            sampleTypes,
            orderables = tests.Concat<object>(profiles).Concat(packages)
        });
    }

    // Client price list for live totals on the registration form (any authenticated user)
    [HttpGet("prices")]
    public async Task<ActionResult<object>> GetPrices([FromQuery] int clientId)
    {
        if (clientId <= 0) return BadRequest("clientId is required.");
        var rates = await _db.ClientRates.AsNoTracking()
            .Where(r => r.ClientId == clientId)
            .Select(r => new { r.ItemType, r.ItemId, r.Rate, r.VatRate })
            .ToListAsync();
        return Ok(rates);
    }

    // Add a payment later
    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult> AddPayment(int id, [FromBody] PaymentCreateDto dto)
    {
        var reg = await _db.Registrations.FindAsync(id);
        if (reg is null) return NotFound();

        var p = new Payment
        {
            RegistrationId = id,
            Method = dto.Method,
            Amount = dto.Amount,
            Reference = dto.Reference,
            Currency = dto.Currency
        };
        _db.Payments.Add(p);

        reg.PaidAmount += dto.Amount;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("items/{registrationItemId:int}/collect")]
    public async Task<ActionResult> CollectSample(int registrationItemId, [FromBody] SampleWorkflowUpdateDto dto)
    {
        var item = await _db.RegistrationItems.FirstOrDefaultAsync(x => x.Id == registrationItemId);
        if (item is null) return NotFound();

        if (item.SampleStatus >= SampleWorkflowStatus.Collected)
            return Conflict("Sample is already collected.");

        item.SampleStatus = SampleWorkflowStatus.Collected;
        item.SampleCollectedAt = DateTime.UtcNow;
        item.SampleNotes = dto.Notes?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("items/{registrationItemId:int}/receive")]
    public async Task<ActionResult> ReceiveSample(int registrationItemId, [FromBody] SampleWorkflowUpdateDto dto)
    {
        var item = await _db.RegistrationItems.FirstOrDefaultAsync(x => x.Id == registrationItemId);
        if (item is null) return NotFound();

        if (item.SampleStatus < SampleWorkflowStatus.Collected)
            return BadRequest("Sample must be collected before receiving.");
        if (item.SampleStatus >= SampleWorkflowStatus.Received)
            return Conflict("Sample is already received.");

        item.SampleStatus = SampleWorkflowStatus.Received;
        item.SampleReceivedAt = DateTime.UtcNow;
        item.SampleNotes = dto.Notes?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("items/{registrationItemId:int}/process")]
    public async Task<ActionResult> ProcessSample(int registrationItemId, [FromBody] SampleWorkflowUpdateDto dto)
    {
        var item = await _db.RegistrationItems.FirstOrDefaultAsync(x => x.Id == registrationItemId);
        if (item is null) return NotFound();

        if (item.SampleStatus < SampleWorkflowStatus.Received)
            return BadRequest("Sample must be received before processing.");
        if (item.SampleStatus >= SampleWorkflowStatus.Processed)
            return Conflict("Sample is already processed.");

        item.SampleStatus = SampleWorkflowStatus.Processed;
        item.SampleProcessedAt = DateTime.UtcNow;
        item.SampleNotes = dto.Notes?.Trim();
        if (item.Status == RegItemStatus.Pending)
            item.Status = RegItemStatus.InProgress;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Simple dashboards
    [HttpGet("dash/pending-tests")]
    public async Task<ActionResult<object>> PendingTests()
    {
        var count = await _db.RegistrationItems.CountAsync(x => x.Status == RegItemStatus.Pending || x.Status == RegItemStatus.InProgress);
        return Ok(new { count });
    }

    [HttpGet("dash/pending-balance")]
    public async Task<ActionResult<object>> PendingBalance()
    {
        var totalBalance = await _db.Registrations.SumAsync(x => x.TotalAmount - x.PaidAmount);
        return Ok(new { totalBalance });
    }

    private async Task<RegistrationDetailDto?> ProjectDetail(int id)
    {
        return await _db.Registrations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RegistrationDetailDto(
                r.Id,
                r.OwnerName,
                r.ClientId,
                r.OwnerId,
                r.DoctorId,
                r.Barcode,
                r.ReferenceNumber,
                r.ClinicalNotes,
                r.CreatedAt,
                r.TotalAmount,
                r.TotalVat,
                r.PaidAmount,
                r.Balance,
                r.Animals.Select(a => new RegistrationAnimalView(a.Id, a.SpeciesId, a.AnimalName, a.AnimalId, a.AgeInDays)).ToList(),
                r.Items.Select(i => new RegistrationItemView(
                    i.Id,
                    i.ItemType,
                    i.ItemId,
                    i.SpeciesId,
                    i.SampleTypeId,
                    i.Price,
                    i.VatRate,
                    i.VatAmount,
                    i.ItemBarcode,
                    i.Status.ToString(),
                    i.SampleStatus.ToString(),
                    i.SampleCollectedAt,
                    i.SampleReceivedAt,
                    i.SampleProcessedAt,
                    i.SampleNotes,
                    i.Quantity)).ToList(),
                r.Payments.Select(p => new PaymentView(p.Id, p.Method, p.Amount, p.PaidAt, p.Reference, p.Currency)).ToList(),
                r.OwnerMobile))
            .FirstOrDefaultAsync();
    }
}
