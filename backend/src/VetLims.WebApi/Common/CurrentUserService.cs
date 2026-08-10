using System.Security.Claims;
using VetLims.Infrastructure;

namespace VetLims.WebApi.Common;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly System.Security.Claims.ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? UserName => _user?.Identity?.Name ?? _user?.FindFirstValue(ClaimTypes.Email);

    public int? OrganizationId
    {
        get
        {
            var v = _user?.FindFirstValue("organization_id");
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public int? ClinicId
    {
        get
        {
            var v = _user?.FindFirstValue("clinic_id");
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public int? BranchId
    {
        get
        {
            var v = _user?.FindFirstValue("branch_id");
            return int.TryParse(v, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<int> BranchIds
    {
        get
        {
            // Optional claim with comma-separated list of branch ids
            var v = _user?.FindFirstValue("branch_ids");
            if (string.IsNullOrWhiteSpace(v))
            {
                if (BranchId.HasValue) return new[] { BranchId.Value };
                return Array.Empty<int>();
            }

            return v.Split(',').Select(s => int.TryParse(s, out var id) ? id : (int?)null).Where(i => i.HasValue).Select(i => i!.Value).ToArray();
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            return _user?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
        }
    }
}
