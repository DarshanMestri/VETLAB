using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Services;
using VetLims.WebApi.DTOs;
using VetLims.WebApi.Reporting;

namespace VetLims.WebApi.Controllers;

[Authorize] // tighten with policy if needed
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

    /// Worklist: filter by test, department, barcode, status
    /// GET /api/results/worklist?departmentId=&testId=&barcode=&status=Pending|InProgress
    [HttpGet("worklist")]
    public async Task<ActionResult<IEnumerable<ResultWorkItemDto>>> Worklist(
        [FromQuery] int? departmentId, [FromQuery] int? testId, [FromQuery] string? barcode, [FromQuery] string? status)
    {
        var q = _db.RegistrationItems
            .AsNoTracking()
            .Include(ri => ri.Registration)
            .Include(ri => ri.Species)
            .Where(ri => ri.ItemType == RegItemType.Test); // result entry per Test items

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var b = barcode.Trim();
            q = q.Where(ri => ri.ItemBarcode == b || ri.Registration.Barcode == b);
        }

        if (testId.HasValue)
            q = q.Where(ri => ri.ItemId == testId.Value);

        if (departmentId.HasValue)
        {
            // Join to Test to filter by Department
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
            // default: show not completed
            q = q.Where(ri => ri.Status == RegItemStatus.Pending || ri.Status == RegItemStatus.InProgress);
        }

        var list = await q
            .OrderBy(ri => ri.RegistrationId).ThenBy(ri => ri.ItemId)
            .Select(ri => new ResultWorkItemDto(
                ri.Id,
                ri.RegistrationId,
                ri.Registration.OwnerName,
                ri.ItemBarcode,
                _db.Tests.Where(t => t.Id == ri.ItemId).Select(t => t.Code).FirstOrDefault()!,
                _db.Tests.Where(t => t.Id == ri.ItemId).Select(t => t.Name).FirstOrDefault()!,
                ri.SpeciesId,
                ri.Species != null ? ri.Species.Name : null,
                ri.Status.ToString(),
                ri.SampleTypeId,
                ri.SampleStatus.ToString(),
                _db.Results.Any(r => r.RegistrationItemId == ri.Id && r.TechnicianVerifiedAt != null),
                _db.Results.Any(r => r.RegistrationItemId == ri.Id && r.PathologistAuthenticatedAt != null)
            ))
            .ToListAsync();

        return Ok(list);
    }

    /// Load details to enter results (existing or blank)
    [HttpGet("{registrationItemId:int}")]
    public async Task<ActionResult<ResultDetailDto>> Get(int registrationItemId)
    {
        var item = await _db.RegistrationItems
            .Include(ri => ri.Registration)
            .ThenInclude(r => r.Animals) // ✅ ensures Animals are loaded
            .FirstOrDefaultAsync(ri => ri.Id == registrationItemId);
        if (item is null) return NotFound();

        // ensure Result row exists
        var result = await _db.Results
            .Include(r => r.Values)
            .FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId);

        if (result is null)
        {
            result = new Result { RegistrationItemId = registrationItemId };
            _db.Results.Add(result);
            await _db.SaveChangesAsync();
        }

        // Build a logical list of parameters from Test
        var parameters = await _db.TestParameters
            .Include(p => p.Unit)
            .Where(p => p.TestId == item.ItemId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var values = new List<ResultValueDto>();
        var ageDays = item.Registration.Animals.FirstOrDefault()?.AgeInDays; // if present
        var speciesId = item.SpeciesId;

        foreach (var p in parameters)
        {
            var existing = result.Values.FirstOrDefault(v => v.TestParameterId == p.Id);

            // compute range context
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

        return Ok(new ResultDetailDto(
            item.Id,
            item.ItemBarcode,
            result.Notes,
            item.SampleStatus.ToString(),
            result.TechnicianVerifiedBy,
            result.TechnicianVerifiedAt,
            result.PathologistAuthenticatedBy,
            result.PathologistAuthenticatedAt,
            result.DigitalSignature,
            result.VerificationRemarks,
            values));
    }

    [HttpGet("{registrationItemId:int}/report")]
    public async Task<ActionResult> Report(int registrationItemId, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        ResultReportFile? report;

        try
        {
            report = await _reports.GenerateAsync(registrationItemId, format, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        if (report is null)
            return NotFound();

        return File(report.Content, report.ContentType, report.FileName);
    }

    /// Save (upsert) values for an item, auto-flag using reference ranges
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

        // update animal age if provided (first animal only for simplicity)
        var firstAnimal = await _db.RegistrationAnimals.FirstOrDefaultAsync(a => a.RegistrationId == item.RegistrationId);
        if (firstAnimal != null && dto.AgeInDays.HasValue) firstAnimal.AgeInDays = dto.AgeInDays;

        var result = await _db.Results.Include(r => r.Values).FirstOrDefaultAsync(r => r.RegistrationItemId == item.Id);
        if (result is null)
        {
            result = new Result { RegistrationItemId = item.Id };
            _db.Results.Add(result);
            await _db.SaveChangesAsync();
            _db.Entry(result).Collection(r => r.Values).Load();
        }

        result.Notes = dto.Notes?.Trim();

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

            var speciesId = item.SpeciesId;
            var ageDays = firstAnimal?.AgeInDays;

            var (minD, maxD, low, high) = await _rr.PickRangeAsync(param.Id, speciesId, ageDays);
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

    /// Mark item completed
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
        var result = await _db.Results.FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId);
        if (result != null) result.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{registrationItemId:int}/verify")]
    public async Task<ActionResult> Verify(int registrationItemId, [FromBody] ResultVerificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.VerifiedBy))
            return BadRequest("VerifiedBy is required.");

        var result = await _db.Results.FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId);
        if (result is null) return NotFound();

        result.TechnicianVerifiedBy = dto.VerifiedBy.Trim();
        result.TechnicianVerifiedAt = DateTime.UtcNow;
        result.VerificationRemarks = dto.Remarks?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{registrationItemId:int}/authenticate")]
    public async Task<ActionResult> Authenticate(int registrationItemId, [FromBody] ResultAuthenticationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AuthenticatedBy))
            return BadRequest("AuthenticatedBy is required.");

        var result = await _db.Results.FirstOrDefaultAsync(r => r.RegistrationItemId == registrationItemId);
        if (result is null) return NotFound();
        if (result.TechnicianVerifiedAt is null)
            return BadRequest("Technician verification is required before authentication.");

        result.PathologistAuthenticatedBy = dto.AuthenticatedBy.Trim();
        result.PathologistAuthenticatedAt = DateTime.UtcNow;
        result.DigitalSignature = dto.DigitalSignature?.Trim();
        result.VerificationRemarks = dto.Remarks?.Trim() ?? result.VerificationRemarks;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
