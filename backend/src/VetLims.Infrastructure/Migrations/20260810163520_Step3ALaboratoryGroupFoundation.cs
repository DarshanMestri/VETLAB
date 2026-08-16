using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Step3ALaboratoryGroupFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "laboratory_groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laboratory_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_groups_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_OrganizationId",
                table: "laboratory_groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_Name",
                table: "laboratory_groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_IsActive",
                table: "laboratory_groups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_OrganizationId_Code",
                table: "laboratory_groups",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "LaboratoryGroupId",
                table: "clinics",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_clinics_LaboratoryGroupId",
                table: "clinics",
                column: "LaboratoryGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_clinics_laboratory_groups_LaboratoryGroupId",
                table: "clinics",
                column: "LaboratoryGroupId",
                principalTable: "laboratory_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clinics_laboratory_groups_LaboratoryGroupId",
                table: "clinics");

            migrationBuilder.DropIndex(
                name: "IX_clinics_LaboratoryGroupId",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "LaboratoryGroupId",
                table: "clinics");

            migrationBuilder.DropIndex(
                name: "IX_laboratory_groups_OrganizationId_Code",
                table: "laboratory_groups");

            migrationBuilder.DropIndex(
                name: "IX_laboratory_groups_IsActive",
                table: "laboratory_groups");

            migrationBuilder.DropIndex(
                name: "IX_laboratory_groups_Name",
                table: "laboratory_groups");

            migrationBuilder.DropIndex(
                name: "IX_laboratory_groups_OrganizationId",
                table: "laboratory_groups");

            migrationBuilder.DropTable(
                name: "laboratory_groups");
        }
    }
}
