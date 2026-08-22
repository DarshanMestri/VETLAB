using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLaboratoryGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_laboratories_laboratory_groups_LaboratoryGroupId",
                table: "laboratories");

            migrationBuilder.DropTable(
                name: "laboratory_groups");

            migrationBuilder.DropIndex(
                name: "IX_laboratories_LaboratoryGroupId",
                table: "laboratories");

            migrationBuilder.DropColumn(
                name: "LaboratoryGroupId",
                table: "laboratories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LaboratoryGroupId",
                table: "laboratories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "laboratory_groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_laboratories_LaboratoryGroupId",
                table: "laboratories",
                column: "LaboratoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_IsActive",
                table: "laboratory_groups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_Name",
                table: "laboratory_groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_OrganizationId",
                table: "laboratory_groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_groups_OrganizationId_Code",
                table: "laboratory_groups",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_laboratories_laboratory_groups_LaboratoryGroupId",
                table: "laboratories",
                column: "LaboratoryGroupId",
                principalTable: "laboratory_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
