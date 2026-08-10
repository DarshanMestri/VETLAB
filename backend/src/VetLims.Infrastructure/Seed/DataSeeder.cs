using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using VetLims.Infrastructure.Identity;

namespace VetLims.Infrastructure.Seed
{
    public class DataSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public DataSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SeedAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // ✅ Ensure roles exist
            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                }
            }

            // ✅ Create admin if missing
            var adminEmail = "admin@vetlims.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    DisplayName = "System Admin",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("✅ Default Admin user created: admin / Admin@123");
                }
                else
                {
                    Console.WriteLine("⚠️ Failed to create default admin: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Admin user already exists.");
            }
        }
    }
}
