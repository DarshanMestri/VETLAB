using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Identity;

namespace VetLims.Infrastructure.Seed
{
    public class DataSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var db = scope.ServiceProvider.GetRequiredService<VetLimsDbContext>();

            await SeedRolesAsync(roleManager);
            await SeedOrganizationHierarchyAsync(db);
            await SeedAdminUserAsync(userManager, db);
        }

        private async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roles = { "Admin", "Doctor", "Laboratory", "Reception", "Manager" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                    _logger.LogInformation("Created role: {Role}", role);
                }
            }
        }

        private async Task SeedOrganizationHierarchyAsync(VetLimsDbContext db)
        {
            // Organization
            var organization = await db.Organizations.FirstOrDefaultAsync(x => x.IsActive);
            if (organization == null)
            {
                organization = new Organization
                {
                    Code = "DEFAULT",
                    Name = "Default Organization",
                    IsActive = true
                };
                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
                _logger.LogInformation("Created default organization: {Code}", organization.Code);
            }

            // Laboratory (required by Branch FK)
            var laboratory = await db.Laboratories.FirstOrDefaultAsync(x => x.OrganizationId == organization.Id && x.IsActive);
            if (laboratory == null)
            {
                laboratory = new Laboratory
                {
                    OrganizationId = organization.Id,
                    Code = "MAINLAB",
                    Name = "Main Laboratory",
                    IsActive = true
                };
                db.Laboratories.Add(laboratory);
                await db.SaveChangesAsSystemAsync(organization.Id);
                _logger.LogInformation("Created default laboratory: {Code}", laboratory.Code);
            }

            // Clinic (required by Branch FK)
            var clinic = await db.Clinics.FirstOrDefaultAsync(x => x.OrganizationId == organization.Id && x.IsActive);
            if (clinic == null)
            {
                clinic = new Clinic
                {
                    OrganizationId = organization.Id,
                    Code = "MAINCLINIC",
                    Name = "Main Clinic",
                    IsActive = true
                };
                db.Clinics.Add(clinic);
                await db.SaveChangesAsSystemAsync(organization.Id);
                _logger.LogInformation("Created default clinic: {Code}", clinic.Code);
            }

            // Branch
            var branch = await db.Branches.FirstOrDefaultAsync(x => x.ClinicId == clinic.Id && x.IsActive);
            if (branch == null)
            {
                branch = new Branch
                {
                    OrganizationId = organization.Id,
                    ClinicId = clinic.Id,
                    LaboratoryId = laboratory.Id,
                    Code = "MAIN",
                    Name = "Main Location",
                    IsActive = true
                };
                db.Branches.Add(branch);
                await db.SaveChangesAsSystemAsync(organization.Id);
                _logger.LogInformation("Created default branch: {Code}", branch.Code);
            }
        }

        private async Task SeedAdminUserAsync(UserManager<AppUser> userManager, VetLimsDbContext db)
        {
            var adminEmail = "admin@vetlims.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var organization = await db.Organizations.FirstOrDefaultAsync(x => x.IsActive);
                var branch = await db.Branches.FirstOrDefaultAsync(x => x.IsActive);

                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    DisplayName = "System Admin",
                    EmailConfirmed = true,
                    IsActive = true,
                    OrganizationId = organization?.Id,
                    BranchId = branch?.Id
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    // Assign admin to default branch with full access
                    if (branch != null && organization != null)
                    {
                        db.UserLocations.Add(new UserLocation
                        {
                            UserId = adminUser.Id,
                            BranchId = branch.Id,
                            IsDefault = true,
                            HasClinicAccess = true,
                            HasLaboratoryAccess = true
                        });
                        await db.SaveChangesAsSystemAsync(organization.Id);
                    }

                    _logger.LogInformation("Created default admin user: admin / Admin@123");
                }
                else
                {
                    _logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
