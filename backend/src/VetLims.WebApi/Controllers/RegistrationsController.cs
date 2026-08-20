using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Common;
using VetLims.WebApi.Reporting;

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
    private readonly BarcodeLabelReportService _barcodeReport;

    public RegistrationsController(VetLimsDbContext db, PricingService pricing, BarcodeService barcodes, ICurrentUserService currentUserService, BarcodeLabelReportService barcodeReport)
    {
        _db = db; _pricing = pricing; _barcodes = barcodes; _currentUserService = currentUserService; _barcodeReport = barcodeReport;
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
        if (q.DateFrom.HasValue) query = query.Where(x => x.CreatedDate >= q.DateFrom.Value);
        if (q.DateTo.HasValue) query = query.Where(x => x.CreatedDate <= q.DateTo.Value.AddDays(1).AddTicks(-1));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new RegistrationDto(
                x.Id,
                x.OwnerName,
                x.OwnerNameAr,
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
                x.OwnerMobile,
                x.DiscountAmount,
                x.DiscountValue,
                (int?)x.DiscountType,
                x.AgeValue,
                (int?)x.AgeUnit,
                x.PaidAmount >= x.TotalAmount ? "Paid" : x.PaidAmount > 0 ? "Partially Paid" : "Unpaid"))
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

        var branch = await _db.Branches
            .FirstOrDefaultAsync(x => x.Id == branchId.Value && x.OrganizationId == organizationId.Value && !x.IsDeleted && x.IsActive);
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

        var ownerNameAr = owner?.NameAr ?? dto.OwnerNameAr?.Trim();

        var reg = new Registration
        {
           LaboratoryId = branch.LaboratoryId,
           BranchId = branch.Id,
           OwnerName = ownerName,
           OwnerNameAr = ownerNameAr,
           OwnerMobile = ownerMobile,
           ClientId = dto.ClientId,
           OwnerId = dto.OwnerId,
           DoctorId = dto.DoctorId,
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            ClinicalNotes = dto.ClinicalNotes?.Trim(),
            Barcode = await _barcodes.NextAsync(),
            AgeValue = dto.AgeValue,
            AgeUnit = dto.AgeUnit
        };

        // Normalize registration-level age to days for reference range lookup
        int? regAgeInDays = null;
        if (dto.AgeValue.HasValue && dto.AgeUnit.HasValue)
        {
            regAgeInDays = dto.AgeUnit.Value switch
            {
                AgeUnit.Days   => dto.AgeValue.Value,
                AgeUnit.Months => dto.AgeValue.Value * 30,
                AgeUnit.Years  => dto.AgeValue.Value * 365,
                _ => dto.AgeValue.Value
            };
        }

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
                Barcode = await _barcodes.NextAsync("AN"),
                AgeInDays = regAgeInDays
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
                LaboratoryId = branch.LaboratoryId,
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

        // Discount calculation (server-authoritative)
        decimal subtotalPlusVat = totalNet + totalVat;
        decimal discountAmount = 0m;
        if (dto.DiscountType.HasValue && dto.DiscountValue > 0)
        {
            if (dto.DiscountType == DiscountType.Percentage)
            {
                if (dto.DiscountValue > 100)
                    return BadRequest("Discount percentage cannot exceed 100%.");
                discountAmount = decimal.Round(subtotalPlusVat * dto.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero);
            }
            else if (dto.DiscountType == DiscountType.Amount)
            {
                if (dto.DiscountValue > subtotalPlusVat)
                    return BadRequest("Discount amount cannot exceed the subtotal.");
                discountAmount = decimal.Round(dto.DiscountValue, 2, MidpointRounding.AwayFromZero);
            }
            if (discountAmount < 0)
                return BadRequest("Discount cannot be negative.");
        }

        reg.DiscountType = dto.DiscountType;
        reg.DiscountValue = decimal.Round(dto.DiscountValue, 2, MidpointRounding.AwayFromZero);
        reg.DiscountAmount = discountAmount;
        reg.TotalAmount = decimal.Round(subtotalPlusVat - discountAmount, 2, MidpointRounding.AwayFromZero);
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RegistrationDetailDto>> Update(int id, [FromBody] RegistrationCreateDto dto)
    {
        var reg = await _db.Registrations
            .Include(x => x.Animals)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (reg is null) return NotFound();

        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue) return Forbid();

        var branchId = dto.BranchId ?? _currentUserService.BranchId;
        if (!branchId.HasValue) return BadRequest("Branch selection is required.");

        var branch = await _db.Branches
            .FirstOrDefaultAsync(x => x.Id == branchId.Value && x.OrganizationId == organizationId.Value && !x.IsDeleted && x.IsActive);
        if (branch is null) return BadRequest("Selected branch is not available for your organization.");

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

        var ownerNameAr = owner?.NameAr ?? dto.OwnerNameAr?.Trim();

        // Update registration header
        reg.LaboratoryId = branch.LaboratoryId;
        reg.BranchId = branch.Id;
        reg.OwnerName = ownerName;
        reg.OwnerNameAr = ownerNameAr;
        reg.OwnerMobile = ownerMobile;
        reg.ClientId = dto.ClientId;
        reg.OwnerId = dto.OwnerId;
        reg.DoctorId = dto.DoctorId;
        reg.ReferenceNumber = dto.ReferenceNumber?.Trim();
        reg.ClinicalNotes = dto.ClinicalNotes?.Trim();
        reg.AgeValue = dto.AgeValue;
        reg.AgeUnit = dto.AgeUnit;

        // Normalize registration-level age to days
        int? regAgeInDays = null;
        if (dto.AgeValue.HasValue && dto.AgeUnit.HasValue)
        {
            regAgeInDays = dto.AgeUnit.Value switch
            {
                AgeUnit.Days => dto.AgeValue.Value,
                AgeUnit.Months => dto.AgeValue.Value * 30,
                AgeUnit.Years => dto.AgeValue.Value * 365,
                _ => dto.AgeValue.Value
            };
        }

        // Clear existing animals and items
        reg.Animals.Clear();
        reg.Items.Clear();
        await _db.SaveChangesAsync(); // Force delete existing

        // Re-add animals
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
                Barcode = await _barcodes.NextAsync("AN"),
                AgeInDays = regAgeInDays
            });
        }

        // Re-add items with pricing
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
                LaboratoryId = branch.LaboratoryId,
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

        // Discount calculation
        decimal subtotalPlusVat = totalNet + totalVat;
        decimal discountAmount = 0m;
        if (dto.DiscountType.HasValue && dto.DiscountValue > 0)
        {
            if (dto.DiscountType == DiscountType.Percentage)
            {
                if (dto.DiscountValue > 100) return BadRequest("Discount percentage cannot exceed 100%.");
                discountAmount = decimal.Round(subtotalPlusVat * dto.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero);
            }
            else if (dto.DiscountType == DiscountType.Amount)
            {
                if (dto.DiscountValue > subtotalPlusVat) return BadRequest("Discount amount cannot exceed the subtotal.");
                discountAmount = decimal.Round(dto.DiscountValue, 2, MidpointRounding.AwayFromZero);
            }
            if (discountAmount < 0) return BadRequest("Discount cannot be negative.");
        }

        reg.DiscountType = dto.DiscountType;
        reg.DiscountValue = decimal.Round(dto.DiscountValue, 2, MidpointRounding.AwayFromZero);
        reg.DiscountAmount = discountAmount;
        reg.TotalAmount = decimal.Round(subtotalPlusVat - discountAmount, 2, MidpointRounding.AwayFromZero);
        reg.TotalVat = decimal.Round(totalVat, 2, MidpointRounding.AwayFromZero);

        await _db.SaveChangesAsync();
        return Ok(await ProjectDetail(reg.Id));
    }

    [HttpGet("lookups")]
    public async Task<ActionResult<object>> GetLookups()
    {
        var owners = await _db.Owners.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, OwnerCode = x.Code, x.Name, x.NameAr, x.Mobile })
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
            .Select(x => new { x.Id, x.Code, x.Name, ItemType = RegItemType.Test, x.DepartmentId, x.SampleTypeId })
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

        if (reg.PaidAmount + dto.Amount > reg.TotalAmount)
            return BadRequest("Payment amount would exceed the total amount due.");

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

    [HttpPost("animals/{animalId:int}/collect")]
    public async Task<ActionResult> CollectAnimalSamples(int animalId, [FromBody] SampleWorkflowUpdateDto dto)
    {
        var animal = await _db.RegistrationAnimals.FirstOrDefaultAsync(x => x.Id == animalId);
        if (animal is null) return NotFound();

        var items = await _db.RegistrationItems
            .Where(x => x.RegistrationId == animal.RegistrationId
                     && x.SpeciesId == animal.SpeciesId
                     && x.SampleStatus < SampleWorkflowStatus.Collected)
            .ToListAsync();

        if (items.Count == 0)
            return Conflict("All samples for this animal are already collected.");

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.SampleStatus = SampleWorkflowStatus.Collected;
            item.SampleCollectedAt = now;
            item.SampleNotes = dto.Notes?.Trim();
        }

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

    [HttpGet("{id:int}/barcodes")]
    public async Task<ActionResult<object>> GetBarcodes(int id)
    {
        var reg = await _db.Registrations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                RegistrationBarcode = r.Barcode,
                OwnerName = r.OwnerName,
                OwnerCode = r.Owner != null ? r.Owner.Code : null,
                Animals = r.Animals
                    .OrderBy(a => a.Id)
                    .Select(a => new
                    {
                        a.Id,
                        a.AnimalName,
                        SpeciesName = a.Species.Name,
                        a.Barcode,
                        a.AgeInDays,
                        AnimalCode = a.Animal != null ? a.Animal.Code : null,
                        a.SpeciesId,
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (reg is null) return NotFound();
        return Ok(reg);
    }

    private async Task<RegistrationDetailDto?> ProjectDetail(int id)
    {
        return await _db.Registrations.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RegistrationDetailDto(
                r.Id,
                r.OwnerName,
                r.OwnerNameAr,
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
                r.Animals.Select(a => new RegistrationAnimalView(a.Id, a.SpeciesId, a.AnimalName, a.AnimalId, a.Barcode, a.AgeInDays, a.Animal != null ? a.Animal.Code : null)).ToList(),
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
                r.OwnerMobile,
                r.DiscountAmount,
                r.DiscountValue,
                (int?)r.DiscountType,
                r.AgeValue,
                (int?)r.AgeUnit,
                r.PaidAmount >= r.TotalAmount ? "Paid" : r.PaidAmount > 0 ? "Partially Paid" : "Unpaid",
                r.Owner != null ? r.Owner.Code : null))
            .FirstOrDefaultAsync();
    }

    [HttpGet("{id:int}/barcodes/pdf")]
    public async Task<IActionResult> GetBarcodesPdf(int id, [FromQuery] int count = 1, CancellationToken ct = default)
    {
        var result = await _barcodeReport.GenerateForRegistrationAsync(id, count, ct);
        if (result is null) return NotFound();
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("animals/{animalId:int}/barcode/pdf")]
    public async Task<IActionResult> GetAnimalBarcodePdf(int animalId, [FromQuery] int count = 1, CancellationToken ct = default)
    {
        var result = await _barcodeReport.GenerateForAnimalAsync(animalId, count, ct);
        if (result is null) return NotFound();
        return File(result.Content, result.ContentType, result.FileName);
    }
}
