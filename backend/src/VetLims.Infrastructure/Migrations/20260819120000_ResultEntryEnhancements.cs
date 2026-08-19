using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResultEntryEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RegistrationItem: re-run tracking fields
            migrationBuilder.AddColumn<string>(
                name: "RerunReason",
                table: "registration_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RerunInitiatedBy",
                table: "registration_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RerunAt",
                table: "registration_items",
                type: "timestamp with time zone",
                nullable: true);

            // Result: per-animal tracking
            migrationBuilder.AddColumn<int?>(
                name: "RegistrationAnimalId",
                table: "results",
                type: "integer",
                nullable: true);

            // Result: tracked user IDs for verification and authentication
            migrationBuilder.AddColumn<int?>(
                name: "TechnicianVerifiedById",
                table: "results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "PathologistAuthenticatedById",
                table: "results",
                type: "integer",
                nullable: true);

            // Foreign key for RegistrationAnimalId on results
            migrationBuilder.AddForeignKey(
                name: "FK_results_registration_animals_RegistrationAnimalId",
                table: "results",
                column: "RegistrationAnimalId",
                principalTable: "registration_animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_results_registration_animals_RegistrationAnimalId",
                table: "results");

            migrationBuilder.DropColumn(
                name: "RerunReason",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "RerunInitiatedBy",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "RerunAt",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "RegistrationAnimalId",
                table: "results");

            migrationBuilder.DropColumn(
                name: "TechnicianVerifiedById",
                table: "results");

            migrationBuilder.DropColumn(
                name: "PathologistAuthenticatedById",
                table: "results");
        }
    }
}
