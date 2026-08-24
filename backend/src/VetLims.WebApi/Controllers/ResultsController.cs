using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Reporting;

namespace VetLims.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly VetLimsDbContext _db;
    private readonly ReferenceRangeService _rr;
    private readonly ResultReportService _reports;
    private readonly ICurrentUserService _currentUserService;

    public ResultsController(VetLimsDbContext db, ReferenceRangeService rr, ResultReportService reports, ICurrentUserService currentUserService)
    {
        _db = db; _rr = rr; _reports = reports; _currentUserService = currentUserService;
    }

    private async Task EnsureAnimalResultsAsync(int registrationItemId, int organizationId)
    {
        var item = await _db.RegistrationItems
            .Include(ri => ri.Registration)
                .ThenInclude(r => r.Animals)
            .FirstOrDefaultAsync(ri => ri.Id == registrationItemId);

        if (item == null) return;

        var existingResults = await _db.Results
            .Where(r => r.RegistrationItemId == registrationItemId)
            .ToListAsync();

        var existingAnimalIds = existingResults
            .Where(r => r.RegistrationAnimalId.HasValue)
            .Select(r => r.RegistrationAnimalId!.Value)
            .ToHashSet();

        foreach (var animal in item.Registration.Animals)
        {
            if (!existingAnimalIds.Contains(animal.Id))
            {
                var newResult = new Result
                {
                    RegistrationItemId = registrationItemId,
                    RegistrationAnimalId = animal.Id,
                    LaboratoryId = item.LaboratoryId,
                    BranchId = item.BranchId,
                    OrganizationId = organizationId,
                    StartedAt = DateTime.UtcNow
                };
                _db.Results.Add(newResult);
            }
        }

        // Also migrate any generic result (no animalId) to first animal if no per-animal results exist yet
        var genericResult = existingResults.FirstOrDefault(r => !r.RegistrationAnimalId.HasValue);
        if (genericResult != null && item.Registration.Animals.Any())
        {
            var firstAnimal = item.Registration.Animals.First();
            if (!existingAnimalIds.Contains(firstAnimal.Id))
            {
                genericResult.RegistrationAnimalId = firstAnimal.Id;
                existingAnimalIds.Add(firstAnimal.Id);
            }
            else
            {
                // First animal already has a result - merge generic result data into existing one
                var existingFirstAnimalResult = existingResults.First(r => r.RegistrationAnimalId == firstAnimal.Id);
                
                // Copy metadata from generic to existing if existing doesn't have it
                if (existingFirstAnimalResult.StartedAt == default && genericResult.StartedAt != default)
                    existingFirstAnimalResult.StartedAt = genericResult.StartedAt;
                if (string.IsNullOrEmpty(existingFirstAnimalResult.Notes) && !string.IsNullOrEmpty(genericResult.Notes))
                    existingFirstAnimalResult.Notes = genericResult.Notes;
                if (existingFirstAnimalResult.TechnicianVerifiedAt == null && genericResult.TechnicianVerifiedAt != null)
                {
                    existingFirstAnimalResult.TechnicianVerifiedBy = genericResult.TechnicianVerifiedBy;
                    existingFirstAnimalResult.TechnicianVerifiedById = genericResult.TechnicianVerifiedById;
                    existingFirstAnimalResult.TechnicianVerifiedAt = genericResult.TechnicianVerifiedAt;
                }
                if (existingFirstAnimalResult.PathologistAuthenticatedAt == null && genericResult.PathologistAuthenticatedAt != null)
                {
                    existingFirstAnimalResult.PathologistAuthenticatedBy = genericResult.PathologistAuthenticatedBy;
                    existingFirstAnimalResult.PathologistAuthenticatedById = genericResult.PathologistAuthenticatedById;
                    existingFirstAnimalResult.PathologistAuthenticatedAt = genericResult.PathologistAuthenticatedAt;
                }
                if (string.IsNullOrEmpty(existingFirstAnimalResult.DigitalSignature) && !string.IsNullOrEmpty(genericResult.DigitalSignature))
                    existingFirstAnimalResult.DigitalSignature = genericResult.DigitalSignature;
                if (string.IsNullOrEmpty(existingFirstAnimalResult.VerificationRemarks) && !string.IsNullOrEmpty(genericResult.VerificationRemarks))
                    existingFirstAnimalResult.VerificationRemarks = genericResult.VerificationRemarks;

                // Copy values from generic to existing (only for parameters not already in existing)
                var genericValues = await _db.ResultValues
                    .Where(v => v.ResultId == genericResult.Id)
                    .ToListAsync();

                foreach (var gv in genericValues)
                {
                    var existingVal = await _db.ResultValues
                        .FirstOrDefaultAsync(v => v.ResultId == existingFirstAnimalResult.Id && v.TestParameterId == gv.TestParameterId);
                    
                    if (existingVal == null)
                    {
                        var newVal = new ResultValue
                        {
                            ResultId = existingFirstAnimalResult.Id,
                            TestParameterId = gv.TestParameterId,
                            EntryType = gv.EntryType,
                            NumericValue = gv.NumericValue,
                            TextValue = gv.TextValue,
                            PositiveNegativeValue = gv.PositiveNegativeValue,
                            FormulaExpression = gv.FormulaExpression,
                            FormulaResult = gv.FormulaResult,
                            ImageUrl = gv.ImageUrl,
                            PdfAttachmentUrl = gv.PdfAttachmentUrl,
                            UnitSymbol = gv.UnitSymbol,
                            IsLow = gv.IsLow,
                            IsHigh = gv.IsHigh,
                            IsOutOfRange = gv.IsOutOfRange,
                            RangeMinDays = gv.RangeMinDays,
                            RangeMaxDays = gv.RangeMaxDays,
                            RangeLow = gv.RangeLow,
                            RangeHigh = gv.RangeHigh
                        };
                        _db.ResultValues.Add(newVal);
                    }
                }

                // Remove generic result
                _db.Results.Remove(genericResult);
            }
        }

        await _db.SaveChangesAsync();
    }

    /// Worklist: filter by test, department, barcode, status, owner
    [HttpGet("worklist")]
    public async Task<ActionResult<IEnumerable<ResultWorkItemDto>>> Worklist(
        [FromQuery] int? departmentId, [FromQuery] int? testId, [FromQuery] string? barcode,
        [FromQuery] string? status, [FromQuery] string? owner, [FromQuery] int? ownerId)
    {
        var q = _db.RegistrationItems
            .AsNoTracking()
            .Include(ri => ri.Registration)
            .ThenInclude(r => r.Animals)
            .ThenInclude(a => a.Species)
            .Include(ri => ri.Species)
            .Where(ri => ri.ItemType == RegItemType.Test);

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var b = barcode.Trim();
            q = q.Where(ri => ri.ItemBarcode == b || ri.Registration.Barcode == b);
        }

        if (ownerId.HasValue)
            q = q.Where(ri => ri.Registration.OwnerId == ownerId.Value);

        if (!string.IsNullOrWhiteSpace(owner))
        {
            var s = $"%{owner.Trim()}%";
            q = q.Where(ri =>
                EF.Functions.ILike(ri.Registration.OwnerName, s) ||
                (ri.Registration.OwnerMobile != null && EF.Functions.ILike(ri.Registration.OwnerMobile, s)) ||
                (ri.Registration.Owner != null && ri.Registration.Owner.Mobile != null && EF.Functions.ILike(ri.Registration.Owner.Mobile, s)));
        }

        if (testId.HasValue)
            q = q.Where(ri => ri.ItemId == testId.Value);

        if (departmentId.HasValue)
        {
            q = q.Where(ri => _db.Tests.Any(t => t.Id == ri.ItemId && t.DepartmentId == departmentId.Value));
        }

        if (!string.IsNullOrEmpty(status))
        {
            var s = status.ToLowerInvariant();
            q = s switch
            {
                "pending"     => q.Where(ri => ri.Status == RegItemStatus.Pending),
                "inprogress"  => q.Where(ri => ri.Status == RegItemStatus.InProgress),
                "completed"   => q.Where(ri => ri.Status == RegItemStatus.Completed),
                _             => q
            };
        }
        else
        {
            q = q.Where(ri => ri.Status == RegItemStatus.Pending || ri.Status == RegItemStatus.InProgress);
        }

        var rows = await q
            .OrderBy(ri => ri.RegistrationId).ThenBy(ri => ri.ItemId)
            .Select(ri => new
            {
                ri.Id,
                ri.RegistrationId,
                ri.Registration.OwnerName,
                ri.Registration.OwnerNameAr,
                ri.Registration.OwnerMobile,
                ri.ItemBarcode,
                TestCode = _db.Tests.Where(t => t.Id == ri.ItemId).Select(t => t.Code).FirstOrDefault()!,
                TestName = _db.Tests.Where(t => t.Id == ri.ItemId).Select(t => t.Name).FirstOrDefault()!,
                ri.SpeciesId,
                SpeciesName = ri.Species != null ? ri.Species.Name : null,
                ri.Status,
                ri.SampleTypeId,
                ri.SampleStatus,
                TechnicianVerified = _db.Results.Any(r => r.RegistrationItemId == ri.Id && r.TechnicianVerifiedAt != null),
                PathologistAuthenticated = _db.Results.Any(r => r.RegistrationItemId == ri.Id && r.PathologistAuthenticatedAt != null),
                AnimalNames = ri.Registration.Animals.Select(a => a.AnimalName).ToList(),
                AnimalSpecies = ri.Registration.Animals.Select(a => a.Species.Name).ToList(),
                AnimalCount = ri.Registration.Animals.Count,
                RerunReason = ri.RerunReason
            })
            .ToListAsync();

        var list = rows.Select(r => new ResultWorkItemDto(
            r.Id,
            r.RegistrationId,
            r.OwnerName,
            r.OwnerNameAr,
            r.OwnerMobile,
            r.ItemBarcode,
            r.TestCode,
            r.TestName,
            r.SpeciesId,
            r.SpeciesName,
            r.Status.ToString(),
            r.SampleTypeId,
            r.SampleStatus.ToString(),
            r.TechnicianVerified,
            r.PathologistAuthenticated,
            string.Join(", ", r.AnimalNames.Zip(r.AnimalSpecies, (n, s) => $"{n} ({s})")),
            r.AnimalCount,
            r.RerunReason
        ));

        return Ok(list);
    }

    /// Load details to enter results (per-animal results)
    [HttpGet("{registrationItemId:int}")]
    public async Task<ActionResult<ResultDetailDto>> Get(int registrationItemId)
    {
        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue)
            return Forbid();

        // Ensure per-animal result rows exist
        await EnsureAnimalResultsAsync(registrationItemId, organizationId.Value);

        var item = await _db.RegistrationItems
            .Include(ri => ri.Registration)
                .ThenInclude(r => r.Owner)
            .Include(ri => ri.Registration)
                .ThenInclude(r => r.Animals)
                    .ThenInclude(a => a.Species)
            .FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        // Load all results for this item (one per animal)
        var results = await _db.Results
            .Include(r => r.Values)
            .Where(r => r.RegistrationItemId == registrationItemId)
            .ToListAsync();

        // Build parameter list from Test
        var parameters = await _db.TestParameters
            .Include(p => p.Unit)
            .Where(p => p.TestId == item.ItemId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        // Build animal results
        var animalResults = new List<AnimalResultDto>();

        foreach (var animal in item.Registration.Animals.OrderBy(a => a.Id))
        {
            var result = results.FirstOrDefault(r => r.RegistrationAnimalId == animal.Id);
            
            var values = new List<ResultValueDto>();
            var ageDays = animal.AgeInDays;
            var speciesId = item.SpeciesId;

            foreach (var p in parameters)
            {
                var existing = result?.Values.FirstOrDefault(v => v.TestParameterId == p.Id);

                var (minD, maxD, low, high) = await _rr.PickRangeAsync(p.Id, speciesId, ageDays);
                decimal? num = existing?.NumericValue;
                var flags = _rr.Evaluate(num, low, high);

                values.Add(new ResultValueDto(
                    p.Id,
                    p.Name,
                    p.Unit?.Symbol,
                    existing?.EntryType ?? ResultEntryType.Numeric,
                    existing?.NumericValue,
                    existing?.TextValue,
                    existing?.PositiveNegativeValue,
                    existing?.FormulaExpression,
                    existing?.FormulaResult,
                    existing?.ImageUrl,
                    existing?.PdfAttachmentUrl,
                    minD, maxD, low, high,
                    flags.isLow, flags.isHigh, flags.outOf
                ));
            }

            animalResults.Add(new AnimalResultDto(
                animal.Id,
                animal.AnimalId ?? 0,
                animal.AnimalName,
                animal.Species?.Name,
                animal.AgeInDays,
                values,
                result?.TechnicianVerifiedAt != null,
                result?.PathologistAuthenticatedAt != null,
                result?.TechnicianVerifiedBy,
                result?.TechnicianVerifiedAt,
                result?.PathologistAuthenticatedBy,
                result?.PathologistAuthenticatedAt,
                result?.DigitalSignature,
                result?.VerificationRemarks,
                result?.Notes
            ));
        }

        var owner = item.Registration.Owner;

        return Ok(new ResultDetailDto(
            item.Id,
            item.ItemBarcode,
            animalResults.FirstOrDefault()?.Notes,
            item.SampleStatus.ToString(),
            item.Status.ToString(),
            animalResults,
            item.Registration.OwnerName,
            item.Registration.OwnerNameAr,
            item.Registration.OwnerMobile,
            owner?.Email,
            owner?.Code,
            item.Registration.OwnerId,
            item.Registration.ClientId,
            item.RerunReason,
            item.RerunInitiatedBy,
            item.RerunAt));
    }

    /// Save (upsert) values for a specific animal, auto-flag using reference ranges
    [HttpPost("{registrationItemId:int}/enter")]
    public async Task<ActionResult> Enter(int registrationItemId, [FromBody] ResultEntryDto dto)
    {
        if (dto.RegistrationItemId != registrationItemId)
            return BadRequest("Mismatched RegistrationItemId.");

        var item = await _db.RegistrationItems.Include(ri => ri.Registration).FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue)
            return Forbid();

        if (item.ItemType == RegItemType.Test && item.BranchId.HasValue)
        {
            var (isAllowed, error) = await TestBranchAvailabilityService.ValidateTestUseAsync(_db, item.ItemId, item.BranchId.Value, organizationId.Value);
            if (!isAllowed)
                return BadRequest(error ?? "The selected Test is not enabled for this Branch.");
        }

        // Move item to InProgress
        if (item.Status == RegItemStatus.Pending)
            item.Status = RegItemStatus.InProgress;
        if (item.SampleStatus < SampleWorkflowStatus.Processing)
            item.SampleStatus = SampleWorkflowStatus.Processing;

        // Ensure per-animal results exist
        await EnsureAnimalResultsAsync(registrationItemId, organizationId.Value);

        // Find the result for the specific animal
        var animalId = dto.RegistrationAnimalId;
        if (!animalId.HasValue)
            return BadRequest("RegistrationAnimalId is required for per-animal result entry.");

        var result = await _db.Results
            .Include(r => r.Values)
            .FirstOrDefaultAsync(r => r.RegistrationItemId == item.Id && r.RegistrationAnimalId == animalId.Value);

        if (result is null)
        {
            return NotFound($"Result for animal {animalId} not found.");
        }

        result.Notes = dto.Notes?.Trim();

        // Get the animal for age/species
        var animal = await _db.RegistrationAnimals.FirstOrDefaultAsync(a => a.Id == animalId.Value);
        var speciesId = item.SpeciesId ?? animal?.SpeciesId;
        var ageDaysForRange = animal?.AgeInDays ?? dto.AgeInDays;

        // Update animal age if provided
        if (animal != null && dto.AgeInDays.HasValue) 
            animal.AgeInDays = dto.AgeInDays;

        // upsert each value and compute flags
        foreach (var v in dto.Values)
        {
            var param = await _db.TestParameters.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == v.TestParameterId);
            if (param is null) return BadRequest($"Invalid TestParameterId: {v.TestParameterId}");

            var rv = result.Values.FirstOrDefault(x => x.TestParameterId == v.TestParameterId);
            if (rv is null)
            {
                rv = new ResultValue { ResultId = result.Id, TestParameterId = param.Id };
                result.Values.Add(rv);
            }

            rv.EntryType = v.EntryType;
            rv.NumericValue = v.NumericValue;
            rv.TextValue = v.TextValue;
            rv.PositiveNegativeValue = v.PositiveNegativeValue?.Trim();
            rv.FormulaExpression = v.FormulaExpression?.Trim();
            rv.FormulaResult = v.FormulaResult?.Trim();
            rv.ImageUrl = v.ImageUrl?.Trim();
            rv.PdfAttachmentUrl = v.PdfAttachmentUrl?.Trim();
            rv.UnitSymbol = param.Unit?.Symbol;

            var (minD, maxD, low, high) = await _rr.PickRangeAsync(param.Id, speciesId, ageDaysForRange);
            rv.RangeMinDays = minD; rv.RangeMaxDays = maxD;
            rv.RangeLow = low; rv.RangeHigh = high;

            var numericForRange = v.EntryType == ResultEntryType.Numeric || v.EntryType == ResultEntryType.Formula
                ? v.NumericValue
                : null;
            var flags = _rr.Evaluate(numericForRange, low, high);
            rv.IsLow = flags.isLow; rv.IsHigh = flags.isHigh; rv.IsOutOfRange = flags.outOf;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Technician verification for a specific animal
    [HttpPost("{registrationItemId:int}/verify")]
    public async Task<ActionResult> Verify(int registrationItemId, [FromBody] ResultVerificationDto dto)
    {
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.UserName;
        if (userId == null || string.IsNullOrWhiteSpace(userName))
            return BadRequest("Authenticated user context is required for verification.");

        if (!dto.RegistrationAnimalId.HasValue)
            return BadRequest("RegistrationAnimalId is required for per-animal verification.");

        var result = await _db.Results
            .FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId && r.RegistrationAnimalId == dto.RegistrationAnimalId.Value);
        if (result is null) return NotFound();

        result.TechnicianVerifiedBy = userName;
        result.TechnicianVerifiedById = userId;
        result.TechnicianVerifiedAt = DateTime.UtcNow;
        result.VerificationRemarks = dto.Remarks?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Bulk verify all animals for this test
    [HttpPost("{registrationItemId:int}/verify-all")]
    public async Task<ActionResult> VerifyAll(int registrationItemId)
    {
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.UserName;
        if (userId == null || string.IsNullOrWhiteSpace(userName))
            return BadRequest("Authenticated user context is required for verification.");

        var results = await _db.Results
            .Where(r => r.RegistrationItemId == registrationItemId && r.RegistrationAnimalId != null)
            .ToListAsync();

        if (!results.Any())
            return NotFound("No results found for this test.");

        foreach (var result in results)
        {
            if (result.TechnicianVerifiedAt == null)
            {
                result.TechnicianVerifiedBy = userName;
                result.TechnicianVerifiedById = userId;
                result.TechnicianVerifiedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Pathologist authentication for a specific animal
    [HttpPost("{registrationItemId:int}/authenticate")]
    public async Task<ActionResult> Authenticate(int registrationItemId, [FromBody] ResultAuthenticationDto dto)
    {
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.UserName;
        if (userId == null || string.IsNullOrWhiteSpace(userName))
            return BadRequest("Authenticated user context is required for authentication.");

        // Only Admin and Manager roles may authenticate (pathologist-level)
        var roles = _currentUserService.Roles;
        if (!roles.Any(r => r == "Admin" || r == "Manager"))
            return Forbid("Only Admin or Manager users may authenticate results.");

        if (!dto.RegistrationAnimalId.HasValue)
            return BadRequest("RegistrationAnimalId is required for per-animal authentication.");

        var item = await _db.RegistrationItems.FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        var result = await _db.Results
            .FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId && r.RegistrationAnimalId == dto.RegistrationAnimalId.Value);
        if (result is null) return NotFound();
        if (result.TechnicianVerifiedAt is null)
            return BadRequest("Technician verification is required before authentication.");

        // Auto-capture authenticated user
        result.PathologistAuthenticatedBy = userName;
        result.PathologistAuthenticatedById = userId;
        result.PathologistAuthenticatedAt = DateTime.UtcNow;
        result.DigitalSignature = dto.DigitalSignature?.Trim();
        result.VerificationRemarks = dto.Remarks?.Trim() ?? result.VerificationRemarks;

        // Check if all animals are now authenticated to complete the test
        var allAnimalResults = await _db.Results
            .Where(r => r.RegistrationItemId == registrationItemId && r.RegistrationAnimalId != null)
            .ToListAsync();

        var allAuthenticated = allAnimalResults.All(r => r.PathologistAuthenticatedAt != null);
        if (allAuthenticated && allAnimalResults.Any())
        {
            item.Status = RegItemStatus.Completed;
            item.SampleStatus = SampleWorkflowStatus.Processed;
            foreach (var r in allAnimalResults)
            {
                if (r.CompletedAt == null)
                    r.CompletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Report generation with optional animal filter and combined option
    [HttpGet("{registrationItemId:int}/report")]
    public async Task<ActionResult> Report(
        int registrationItemId, 
        [FromQuery] string? format, 
        [FromQuery] int? animalId,
        [FromQuery] bool combined = false,
        CancellationToken cancellationToken = default)
    {
        ResultReportFile? report;

        try
        {
            report = await _reports.GenerateAsync(registrationItemId, format, animalId, combined, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (report is null)
            return NotFound();

        return File(report.Content, report.ContentType, report.FileName);
    }

    /// Re-run: move a completed/authenticated result back to InProgress for the technician
    [HttpPost("{registrationItemId:int}/rerun")]
    public async Task<ActionResult> Rerun(int registrationItemId, [FromBody] ResultRerunDto dto)
    {
        var roles = _currentUserService.Roles;
        if (!roles.Any(r => r == "Admin" || r == "Manager"))
            return Forbid("Only Admin or Manager users may initiate a re-run.");

        var item = await _db.RegistrationItems.FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        if (item.Status != RegItemStatus.Completed)
            return BadRequest("Only completed results can be moved back to pending for re-run.");

        var userName = _currentUserService.UserName ?? "system";

        // Reset status back to InProgress for the technician workflow
        item.Status = RegItemStatus.InProgress;

        // Preserve re-run audit trail
        item.RerunReason = dto.Reason?.Trim();
        item.RerunInitiatedBy = userName;
        item.RerunAt = DateTime.UtcNow;

        // Clear authentication/verification for ALL animals so they must be re-done
        var results = await _db.Results
            .Where(r => r.RegistrationItemId == registrationItemId)
            .ToListAsync();

        foreach (var result in results)
        {
            result.TechnicianVerifiedBy = null;
            result.TechnicianVerifiedById = null;
            result.TechnicianVerifiedAt = null;
            result.PathologistAuthenticatedBy = null;
            result.PathologistAuthenticatedById = null;
            result.PathologistAuthenticatedAt = null;
            result.CompletedAt = null;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// Mark item completed (kept for backward compatibility, but deprecated in favor of authenticate auto-complete)
    [HttpPost("{registrationItemId:int}/complete")]
    public async Task<ActionResult> Complete(int registrationItemId)
    {
        var item = await _db.RegistrationItems.FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        var organizationId = _currentUserService.OrganizationId;
        if (!organizationId.HasValue)
            return Forbid();

        if (item.ItemType == RegItemType.Test && item.BranchId.HasValue)
        {
            var (isAllowed, error) = await TestBranchAvailabilityService.ValidateTestUseAsync(_db, item.ItemId, item.BranchId.Value, organizationId.Value);
            if (!isAllowed)
                return BadRequest(error ?? "The selected Test is not enabled for this Branch.");
        }

        item.Status = RegItemStatus.Completed;
        item.SampleStatus = SampleWorkflowStatus.Processed;
        
        var results = await _db.Results
            .Where(r => r.RegistrationItemId == registrationItemId && r.RegistrationAnimalId != null)
            .ToListAsync();
        
        foreach (var result in results)
        {
            if (result.CompletedAt == null)
                result.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}