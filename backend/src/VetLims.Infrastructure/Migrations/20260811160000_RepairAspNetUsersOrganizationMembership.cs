using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations;

/// <summary>
/// Retains the historical migration identity after reconciliation. MultiLabPhase1
/// owns the AspNetUsers organization membership column, index, and foreign key.
/// </summary>
[DbContext(typeof(VetLimsDbContext))]
[Migration("20260811160000_RepairAspNetUsersOrganizationMembership")]
public partial class RepairAspNetUsersOrganizationMembership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty after historical ownership was restored to MultiLabPhase1.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty because this migration no longer owns schema objects.
    }
}
