using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    public partial class MultiLabPhase1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Organizations: new columns
            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipOrPin",
                table: "organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Clinics (Laboratory Master): new columns
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "clinics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "clinics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "clinics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSettingsJson",
                table: "clinics",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "clinics",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Branches (Location): new columns
            migrationBuilder.AddColumn<int>(
                name: "LocationType",
                table: "branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "branches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "branches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manager",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // AspNetUsers: OrganizationId and keep ClinicId/BranchId already exist in earlier migrations
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create user_locations table to map users to multiple branches
            migrationBuilder.CreateTable(
                name: "user_locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_locations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_locations_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_locations_UserId_BranchId",
                table: "user_locations",
                columns: new[] { "UserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_locations_UserId",
                table: "user_locations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_locations_BranchId",
                table: "user_locations",
                column: "BranchId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_locations");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "State",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ZipOrPin",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "EmailSettingsJson",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "City",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "State",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Pin",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Manager",
                table: "branches");
        }
    }
}
