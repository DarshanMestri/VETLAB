using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Step4B_TestOrganizationScope_SoftDelete_BranchTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tests_Code",
                table: "tests");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "user_locations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "user_locations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UnixEpoch);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "user_locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "whatsapp_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "whatsapp_message_queue",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "vaccination_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "user_refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "user_locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "user_activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "tests",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tests
                SET "OrganizationId" = 0
                WHERE "OrganizationId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "tests",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "tests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "test_reference_ranges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "test_parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "species",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "screen_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "sample_types",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "role_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "results",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "result_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "registration_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "registration_animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "queue_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "prescription_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "packages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "package_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "owners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "methods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "menu_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "medicines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "medical_history_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "login_histories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "laboratory_groups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_purchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_purchase_returns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_purchase_return_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_purchase_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_consumptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "inventory_categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "instruments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "genders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "departments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "containers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "clinics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "clinical_encounters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "client_rates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "button_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "breeds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "branches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "branch_departments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "billing_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "billing_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "billing_invoice_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "barcode_sequences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "animal_colors",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branch_tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branch_tests_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_branch_tests_tests_TestId",
                        column: x => x.TestId,
                        principalTable: "tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tests_OrganizationId",
                table: "tests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_tests_OrganizationId_Code",
                table: "tests",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_tests_BranchId",
                table: "branch_tests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_branch_tests_BranchId_TestId",
                table: "branch_tests",
                columns: new[] { "BranchId", "TestId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_branch_tests_TestId",
                table: "branch_tests",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_tests");

            migrationBuilder.DropIndex(
                name: "IX_tests_OrganizationId",
                table: "tests");

            migrationBuilder.DropIndex(
                name: "IX_tests_OrganizationId_Code",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "user_locations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "user_locations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "user_locations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "whatsapp_templates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "whatsapp_message_queue");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "vaccination_records");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "user_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "user_locations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "user_activity_logs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "units");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "test_reference_ranges");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "test_parameters");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "species");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "screen_permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "sample_types");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "role_permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "results");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "registration_animals");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "queue_entries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "prescription_items");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "packages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "package_items");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "owners");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "methods");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "menu_permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "medicines");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "medical_history_entries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "login_histories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "laboratory_groups");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_purchases");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_purchase_returns");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_purchase_return_lines");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_purchase_lines");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_consumptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "inventory_categories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "instruments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "genders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "doctors");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "containers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "clinical_encounters");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "client_rates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "button_permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "breeds");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "branch_departments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "billing_transactions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "billing_invoices");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "billing_invoice_lines");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "barcode_sequences");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "animal_colors");

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "tests",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_tests_Code",
                table: "tests",
                column: "Code",
                unique: true);
        }
    }
}
