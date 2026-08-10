using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationHierarchyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "Id", "Code", "Name", "LegalName", "Email", "Phone", "Address", "IsActive", "CreatedDate", "CreatedBy", "ModifiedDate", "ModifiedBy", "IsDeleted", "DeletedDate", "CreatedAt", "UpdatedAt" },
                values: new object[] { 1, "DEFAULT", "Default Organization", null, null, null, null, true, new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc), "system", null, null, false, null, new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc), null });
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('organizations', 'Id'), GREATEST((SELECT COALESCE(MAX(\"Id\"), 1) FROM organizations), 1));");

            migrationBuilder.DropIndex(
                name: "IX_clinics_Code",
                table: "clinics");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "clinics",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_clinics_OrganizationId_Code",
                table: "clinics",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Code",
                table: "organizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Name",
                table: "organizations",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_clinics_organizations_OrganizationId",
                table: "clinics",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clinics_organizations_OrganizationId",
                table: "clinics");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_clinics_OrganizationId_Code",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "clinics");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_Code",
                table: "clinics",
                column: "Code",
                unique: true);
        }
    }
}
