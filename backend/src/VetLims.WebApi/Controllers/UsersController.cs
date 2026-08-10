// Step 4: Admin-only user ops
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _users;

    public UsersController(UserManager<AppUser> users)
    {
        _users = users;
    }

    public record RoleRequest(string RoleName);
    public record ToggleActiveRequest(bool IsActive);

    [HttpGet]
    public ActionResult<IEnumerable<object>> List()
    {
        // For large systems, project/query via EF. For now, simple projection:
        var data = _users.Users.Select(u => new { u.Id, u.UserName, u.Email, u.DisplayName, u.IsActive }).ToList();
        return Ok(data);
    }

    [HttpPost("{userId:int}/roles")]
    public async Task<ActionResult> AddRole(int userId, [FromBody] RoleRequest req)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var res = await _users.AddToRoleAsync(user, req.RoleName);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }

    [HttpDelete("{userId:int}/roles/{roleName}")]
    public async Task<ActionResult> RemoveRole(int userId, string roleName)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var res = await _users.RemoveFromRoleAsync(user, roleName);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }

    [HttpPatch("{userId:int}/active")]
    public async Task<ActionResult> ToggleActive(int userId, [FromBody] ToggleActiveRequest req)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        user.IsActive = req.IsActive;
        var res = await _users.UpdateAsync(user);
        if (!res.Succeeded) return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));
        return Ok();
    }
}
