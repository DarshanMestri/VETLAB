using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Controllers;

/// <summary>
/// Explicit per-user branch access (UserLocation) administration.
/// Access is never implied: every assignment is stored, can be individually
/// activated/deactivated, and duplicates are impossible thanks to the
/// unique (UserId, BranchId) index combined with upsert semantics.
/// </summary>
[ApiController]
[Route("api/users/{userId:int}/locations")]
[Authorize(Policy = "RequireAdmin")]
public class UserLocationsController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly VetLimsDbContext _db;

    public record BranchAccessItem(
        int BranchId,
        bool IsActive,
        bool HasClinicAccess,
        bool HasLaboratoryAccess,
        bool IsDefault);
    public record SaveBranchAccessRequest(IReadOnlyList<BranchAccessItem> Accesses);
    public record BranchAccessDto(
        int Id,
        int BranchId,
        string BranchName,
        string LaboratoryName,
        string ClinicName,
        bool IsActive,
        bool HasClinicAccess,
        bool HasLaboratoryAccess,
        bool IsDefault);

    public UserLocationsController(UserManager<AppUser> users, VetLimsDbContext db)
    {
        _users = users;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BranchAccessDto>>> Get(int userId)
    {
        var user = await FindUserAsync(userId);
        if (user == null) return NotFound("User not found.");

        return Ok(await LoadAsync(userId));
    }

    /// <summary>
    /// Replaces the user's complete branch-access set.
    /// Existing rows are updated in place (upsert); rows missing from the payload
    /// are soft-deleted so the unique index keeps guaranteeing one row per pair.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<BranchAccessDto>>> Save(
        int userId,
        [FromBody] SaveBranchAccessRequest request)
    {
        var user = await FindUserAsync(userId);
        if (user == null) return NotFound("User not found.");
        if (!user.OrganizationId.HasValue)
            return BadRequest("The target user is not assigned to an organization.");

        var items = request.Accesses ?? new List<BranchAccessItem>();
        if (items.Select(a => a.BranchId).Distinct().Count() != items.Count)
            return BadRequest("Duplicate branch entries are not allowed.");

        if (items.Count(a => a.IsDefault) > 1)
            return BadRequest("Only one branch can be marked as default.");

        var branchIds = items.Select(a => a.BranchId).ToList();
        // Branches may span multiple organizations: each granted branch is
        // validated on its own and stamps its organization onto the row, so a
        // non-admin user can hold access across the whole estate.
        var validBranches = await _db.Branches.AsNoTracking()
            .Where(b => branchIds.Contains(b.Id) && b.IsActive && !b.IsDeleted)
            .ToDictionaryAsync(b => b.Id, b => b.OrganizationId!.Value);

        var invalid = branchIds.Except(validBranches.Keys).ToList();
        if (invalid.Count > 0)
            return BadRequest($"Branch(es) {string.Join(", ", invalid)} are invalid or inactive.");

        // Admins implicitly hold every branch of their organization.
        var roles = await _users.GetRolesAsync(user);
        var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                      roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
        if (isAdmin)
            return BadRequest("Administrator accounts already have access to every branch of their organization.");

        var existing = await _db.UserLocations
            .Where(ul => ul.UserId == userId)
            .ToListAsync();

        // Branches may span multiple organizations, and every UserLocation row
        // carries its branch's organization. The ownership validator checks all
        // dirty entries against a single execution organization, so mutations are
        // applied and flushed one organization group at a time.
        var payloadIds = items.Select(a => a.BranchId).ToHashSet();
        int RowOrg(UserLocation row) => row.OrganizationId ?? user.OrganizationId!.Value;

        foreach (var org in items.Select(i => validBranches[i.BranchId])
                     .Concat(existing.Where(ul => !payloadIds.Contains(ul.BranchId) && !ul.IsDeleted).Select(RowOrg))
                     .Distinct())
        {
            foreach (var item in items.Where(i => validBranches[i.BranchId] == org))
            {
                var row = existing.FirstOrDefault(ul => ul.BranchId == item.BranchId);
                if (row == null)
                {
                    row = new UserLocation
                    {
                        UserId = userId,
                        BranchId = item.BranchId,
                        CreatedBy = CurrentUserName(),
                    };
                    _db.UserLocations.Add(row);
                    existing.Add(row);
                }

                row.IsActive = item.IsActive;
                row.HasClinicAccess = item.HasClinicAccess;
                row.HasLaboratoryAccess = item.HasLaboratoryAccess;
                row.IsDefault = item.IsDefault;
                row.OrganizationId = validBranches[item.BranchId];   // keep in sync with the branch's current organization
                row.IsDeleted = false;              // re-activating a previously removed pair
                row.DeletedDate = null;
                row.DeletedBy = null;
                row.ModifiedDate = DateTime.UtcNow;
                row.ModifiedBy = CurrentUserName();
            }

            // Payload is authoritative: anything not included loses its access.
            foreach (var row in existing.Where(ul =>
                         !payloadIds.Contains(ul.BranchId) && !ul.IsDeleted && RowOrg(ul) == org))
            {
                row.IsDeleted = true;
                row.IsActive = false;
                row.DeletedDate = DateTime.UtcNow;
                row.DeletedBy = CurrentUserName();
                row.ModifiedDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsSystemAsync(org);
        }

        return Ok(await LoadAsync(userId));
    }

    private async Task<AppUser?> FindUserAsync(int userId) =>
        await _users.FindByIdAsync(userId.ToString());

    private async Task<List<BranchAccessDto>> LoadAsync(int userId) =>
        await _db.UserLocations.AsNoTracking()
            .Where(ul => ul.UserId == userId && !ul.IsDeleted)
            .OrderByDescending(ul => ul.IsDefault)
            .ThenBy(ul => ul.Branch.Name)
            .Select(ul => new BranchAccessDto(
                ul.Id,
                ul.BranchId,
                ul.Branch!.Name,
                ul.Branch.Laboratory!.Name,
                ul.Branch.Clinic!.Name,
                ul.IsActive,
                ul.HasClinicAccess,
                ul.HasLaboratoryAccess,
                ul.IsDefault))
            .ToListAsync();

    private string? CurrentUserName() => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
}
