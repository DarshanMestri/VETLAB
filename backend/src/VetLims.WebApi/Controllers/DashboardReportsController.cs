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
public class DashboardReportsController : ControllerBase
{
    private readonly VetLimsDbContext _db;

    public DashboardReportsController(VetLimsDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard/admin")]
    public async Task<ActionResult<DashboardSectionDto>> AdminDashboard(CancellationToken ct)
    {
        var revenue = await _db.BillingInvoices.SumAsync(x => (decimal?)x.PaymentAmount, ct) ?? 0m;
        var outstanding = await _db.BillingInvoices.SumAsync(x => (decimal?)x.OutstandingAmount, ct) ?? 0m;
        var pendingReports = await _db.RegistrationItems.CountAsync(x => x.Status == RegItemStatus.Pending || x.Status == RegItemStatus.InProgress, ct);
        var lowStock = await _db.InventoryPurchaseLines.CountAsync(x => x.AvailableQuantity <= x.LowStockThreshold, ct);

        return Ok(new DashboardSectionDto("Admin", new[]
        {
            new DashboardKpiDto("Revenue Collected", revenue, "AED"),
            new DashboardKpiDto("Outstanding", outstanding, "AED"),
            new DashboardKpiDto("Pending Reports", pendingReports),
            new DashboardKpiDto("Low Stock Alerts", lowStock)
        }));
    }

    [HttpGet("dashboard/doctor")]
    public async Task<ActionResult<DashboardSectionDto>> DoctorDashboard(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var todaysAppointments = await _db.Appointments.CountAsync(x => x.ScheduledAt >= today && x.ScheduledAt < tomorrow && !x.IsDeleted, ct);
        var consultationsDone = await _db.Appointments.CountAsync(x => x.Status == AppointmentStatus.Completed && x.ScheduledAt >= today.AddDays(-30) && !x.IsDeleted, ct);
        var followUps = await _db.ClinicalEncounters.CountAsync(x => x.FollowUpAt.HasValue && x.FollowUpAt >= today && !x.IsDeleted, ct);
        var prescriptions = await _db.PrescriptionItems.CountAsync(x => !x.IsDeleted && x.CreatedDate >= today, ct);

        return Ok(new DashboardSectionDto("Doctor", new[]
        {
            new DashboardKpiDto("Today's Appointments", todaysAppointments),
            new DashboardKpiDto("Consultations (30d)", consultationsDone),
            new DashboardKpiDto("Upcoming Follow Ups", followUps),
            new DashboardKpiDto("Today's Prescriptions", prescriptions)
        }));
    }

    [HttpGet("dashboard/laboratory")]
    public async Task<ActionResult<DashboardSectionDto>> LaboratoryDashboard(CancellationToken ct)
    {
        var pendingSamples = await _db.RegistrationItems.CountAsync(x => x.SampleStatus == SampleWorkflowStatus.Collected || x.SampleStatus == SampleWorkflowStatus.Received || x.SampleStatus == SampleWorkflowStatus.Processing, ct);
        var pendingResults = await _db.RegistrationItems.CountAsync(x => x.Status == RegItemStatus.Pending || x.Status == RegItemStatus.InProgress, ct);
        var verified = await _db.Results.CountAsync(x => x.TechnicianVerifiedAt != null, ct);
        var authenticated = await _db.Results.CountAsync(x => x.PathologistAuthenticatedAt != null, ct);

        return Ok(new DashboardSectionDto("Laboratory", new[]
        {
            new DashboardKpiDto("Pending Samples", pendingSamples),
            new DashboardKpiDto("Pending Results", pendingResults),
            new DashboardKpiDto("Technician Verified", verified),
            new DashboardKpiDto("Pathologist Authenticated", authenticated)
        }));
    }

    [HttpGet("dashboard/reception")]
    public async Task<ActionResult<DashboardSectionDto>> ReceptionDashboard(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var todayAppts = await _db.Appointments.CountAsync(x => x.ScheduledAt >= today && x.ScheduledAt < tomorrow && !x.IsDeleted, ct);
        var queueWaiting = await _db.QueueEntries.CountAsync(x => x.Status == QueueStatus.Waiting || x.Status == QueueStatus.Called || x.Status == QueueStatus.InConsultation, ct);
        var walkIns = await _db.Appointments.CountAsync(x => (x.Type == AppointmentType.WalkIn || x.Type == AppointmentType.Emergency) && x.ScheduledAt >= today && x.ScheduledAt < tomorrow && !x.IsDeleted, ct);
        var pendingBalance = await _db.Registrations.SumAsync(x => (decimal?)(x.TotalAmount - x.PaidAmount), ct) ?? 0m;

        return Ok(new DashboardSectionDto("Reception", new[]
        {
            new DashboardKpiDto("Today's Appointments", todayAppts),
            new DashboardKpiDto("Queue Waiting", queueWaiting),
            new DashboardKpiDto("Walk-in / Emergency", walkIns),
            new DashboardKpiDto("Pending Balance", pendingBalance, "AED")
        }));
    }

    [HttpGet("reports/revenue")]
    public async Task<ActionResult<object>> RevenueReport([FromQuery] ReportQuery q, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize, max: 200);
        var from = q.From ?? DateTime.UtcNow.Date.AddMonths(-1);
        var to = q.To ?? DateTime.UtcNow;

        var query = _db.BillingInvoices
            .AsNoTracking()
            .Include(x => x.Owner)
            .Where(x => x.InvoiceDate >= from && x.InvoiceDate <= to);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.InvoiceNumber, s) || (x.Owner != null && EF.Functions.ILike(x.Owner.Name, s)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.InvoiceDate)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new RevenueReportRowDto(x.Id, x.InvoiceNumber, x.InvoiceDate, x.Owner != null ? x.Owner.Name : null, x.NetAmount, x.PaymentAmount, x.OutstandingAmount, x.Currency))
            .ToListAsync(ct);

        var summary = await query.GroupBy(_ => 1).Select(g => new RevenueReportSummaryDto(
            g.Sum(x => x.NetAmount), g.Sum(x => x.PaymentAmount), g.Sum(x => x.OutstandingAmount)))
            .FirstOrDefaultAsync(ct) ?? new RevenueReportSummaryDto(0, 0, 0);

        return Ok(new { page, pageSize = size, total, summary, items = rows });
    }

    [HttpGet("reports/pending")]
    public async Task<ActionResult<PagedResult<PendingReportRowDto>>> PendingReports([FromQuery] ReportQuery q, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize, max: 200);
        var query = _db.RegistrationItems
            .AsNoTracking()
            .Include(x => x.Registration)
            .Where(x => x.Status == RegItemStatus.Pending || x.Status == RegItemStatus.InProgress);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.ItemBarcode, s) || EF.Functions.ILike(x.Registration.Barcode, s) || EF.Functions.ILike(x.Registration.OwnerName, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new PendingReportRowDto(
                x.Id,
                x.RegistrationId,
                x.Registration.Barcode,
                x.Registration.OwnerName,
                _db.Tests.Where(t => t.Id == x.ItemId).Select(t => t.Name).FirstOrDefault(),
                x.Status,
                x.SampleStatus,
                x.CreatedDate))
            .ToListAsync(ct);

        return Ok(new PagedResult<PendingReportRowDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpGet("reports/test-statistics")]
    public async Task<ActionResult<IEnumerable<TestStatisticDto>>> TestStatistics(CancellationToken ct)
    {
        var grouped = await _db.RegistrationItems
            .AsNoTracking()
            .Where(x => x.ItemType == RegItemType.Test)
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                TestId = g.Key,
                OrderedCount = g.Count(),
                CompletedCount = g.Count(x => x.Status == RegItemStatus.Completed)
            })
            .OrderByDescending(x => x.OrderedCount)
            .Take(100)
            .ToListAsync(ct);

        var testNames = await _db.Tests
            .AsNoTracking()
            .Where(x => grouped.Select(g => g.TestId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var stats = grouped
            .Select(x => new TestStatisticDto(
                testNames.TryGetValue(x.TestId, out var name) ? name : $"Test {x.TestId}",
                x.OrderedCount,
                x.CompletedCount))
            .ToList();

        return Ok(stats);
    }

    [HttpGet("reports/species-statistics")]
    public async Task<ActionResult<IEnumerable<SpeciesStatisticDto>>> SpeciesStatistics(CancellationToken ct)
    {
        var regCounts = await _db.RegistrationAnimals
            .AsNoTracking()
            .GroupBy(x => x.SpeciesId)
            .Select(g => new { SpeciesId = g.Key, RegistrationCount = g.Count() })
            .ToListAsync(ct);

        var testCounts = await _db.RegistrationItems
            .AsNoTracking()
            .Where(x => x.SpeciesId.HasValue)
            .GroupBy(x => x.SpeciesId!.Value)
            .Select(g => new { SpeciesId = g.Key, TestCount = g.Count() })
            .ToListAsync(ct);

        var speciesMap = await _db.Species.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var result = regCounts
            .Select(x => new SpeciesStatisticDto(
                speciesMap.TryGetValue(x.SpeciesId, out var name) ? name : $"Species {x.SpeciesId}",
                x.RegistrationCount,
                testCounts.FirstOrDefault(t => t.SpeciesId == x.SpeciesId)?.TestCount ?? 0))
            .OrderByDescending(x => x.TestCount)
            .Take(100)
            .ToList();

        return Ok(result);
    }

    [HttpGet("reports/inventory")]
    public async Task<ActionResult<PagedResult<InventoryReportRowDto>>> InventoryReport([FromQuery] ReportQuery q, [FromQuery] bool lowStockOnly = false, CancellationToken ct = default)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize, max: 200);
        var query = _db.InventoryPurchaseLines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.BatchNumber, s) || EF.Functions.ILike(x.Medicine.Name, s));
        }
        if (lowStockOnly)
            query = query.Where(x => x.AvailableQuantity <= x.LowStockThreshold);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.ExpiryDate)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new InventoryReportRowDto(
                x.Id,
                x.Medicine.Name,
                x.BatchNumber,
                x.ExpiryDate,
                x.Quantity,
                x.AvailableQuantity,
                x.LowStockThreshold,
                x.AvailableQuantity <= x.LowStockThreshold,
                x.ExpiryDate.HasValue && x.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date)))
            .ToListAsync(ct);

        return Ok(new PagedResult<InventoryReportRowDto> { Page = page, PageSize = size, Total = total, Items = items });
    }

    [HttpGet("reports/audit")]
    public async Task<ActionResult<PagedResult<AuditReportRowDto>>> AuditReport([FromQuery] ReportQuery q, CancellationToken ct)
    {
        var (page, size) = PagingHelpers.Normalize(q.Page, q.PageSize, max: 200);
        var query = _db.AuditLogs.AsNoTracking();

        if (q.From.HasValue) query = query.Where(x => x.ChangedAt >= q.From.Value);
        if (q.To.HasValue) query = query.Where(x => x.ChangedAt <= q.To.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = $"%{q.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.TableName, s) ||
                EF.Functions.ILike(x.Action, s) ||
                (x.ChangedBy != null && EF.Functions.ILike(x.ChangedBy, s)) ||
                EF.Functions.ILike(x.RecordId, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.ChangedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new AuditReportRowDto(x.Id, x.TableName, x.RecordId, x.Action, x.ChangedBy, x.ChangedAt))
            .ToListAsync(ct);

        return Ok(new PagedResult<AuditReportRowDto> { Page = page, PageSize = size, Total = total, Items = items });
    }
}
