// Step 4: Seed default roles + admin user
using Microsoft.AspNetCore.Identity;
using VetLims.Infrastructure.Identity;

namespace VetLims.WebApi.Seed;

public class IdentitySeeder
{
    private readonly RoleManager<AppRole> _roles;
    private readonly UserManager<AppUser> _users;

    public IdentitySeeder(RoleManager<AppRole> roles, UserManager<AppUser> users)
    {
        _roles = roles;
        _users = users;
    }

    public async Task SeedAsync()
    {
        string[] roleNames = ["Admin", "Tech", "FrontDesk", "Manager"];

        foreach (var r in roleNames)
        {
            if (!await _roles.RoleExistsAsync(r))
                await _roles.CreateAsync(new AppRole { Name = r });
        }

        var adminEmail = "admin@vetlims.local";
        var admin = await _users.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new AppUser
            {
                UserName = "admin",
                Email = adminEmail,
                DisplayName = "System Admin",
                EmailConfirmed = true
            };
            var result = await _users.CreateAsync(admin, "Admin@123"); // dev password
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
