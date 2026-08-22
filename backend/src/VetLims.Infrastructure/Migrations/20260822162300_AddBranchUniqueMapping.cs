using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchUniqueMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branches_ClinicId",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branches_LaboratoryId",
                table: "branches");

            migrationBuilder.CreateIndex(
                name: "IX_branches_ClinicId",
                table: "branches",
                column: "ClinicId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_branches_LaboratoryId",
                table: "branches",
                column: "LaboratoryId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branches_ClinicId",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branches_LaboratoryId",
                table: "branches");

            migrationBuilder.CreateIndex(
                name: "IX_branches_ClinicId",
                table: "branches",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_branches_LaboratoryId",
                table: "branches",
                column: "LaboratoryId");
        }
    }
}
