using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiClinicTransactionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "result_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "result_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "registration_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "registration_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "client_rates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "client_rates",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_results_BranchId",
                table: "results",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_results_ClinicId",
                table: "results",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_result_values_BranchId",
                table: "result_values",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_result_values_ClinicId",
                table: "result_values",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_BranchId",
                table: "registrations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_ClinicId",
                table: "registrations",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_items_BranchId",
                table: "registration_items",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_items_ClinicId",
                table: "registration_items",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_BranchId",
                table: "payments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ClinicId",
                table: "payments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_client_rates_BranchId",
                table: "client_rates",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_client_rates_ClinicId",
                table: "client_rates",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BranchId",
                table: "AspNetUsers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_branches_BranchId",
                table: "AspNetUsers",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_clinics_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_client_rates_branches_BranchId",
                table: "client_rates",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_client_rates_clinics_ClinicId",
                table: "client_rates",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_branches_BranchId",
                table: "payments",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_clinics_ClinicId",
                table: "payments",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registration_items_branches_BranchId",
                table: "registration_items",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registration_items_clinics_ClinicId",
                table: "registration_items",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_branches_BranchId",
                table: "registrations",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_clinics_ClinicId",
                table: "registrations",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_result_values_branches_BranchId",
                table: "result_values",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_result_values_clinics_ClinicId",
                table: "result_values",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_results_branches_BranchId",
                table: "results",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_results_clinics_ClinicId",
                table: "results",
                column: "ClinicId",
                principalTable: "clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_branches_BranchId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_clinics_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_client_rates_branches_BranchId",
                table: "client_rates");

            migrationBuilder.DropForeignKey(
                name: "FK_client_rates_clinics_ClinicId",
                table: "client_rates");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_branches_BranchId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_clinics_ClinicId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_registration_items_branches_BranchId",
                table: "registration_items");

            migrationBuilder.DropForeignKey(
                name: "FK_registration_items_clinics_ClinicId",
                table: "registration_items");

            migrationBuilder.DropForeignKey(
                name: "FK_registrations_branches_BranchId",
                table: "registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_registrations_clinics_ClinicId",
                table: "registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_result_values_branches_BranchId",
                table: "result_values");

            migrationBuilder.DropForeignKey(
                name: "FK_result_values_clinics_ClinicId",
                table: "result_values");

            migrationBuilder.DropForeignKey(
                name: "FK_results_branches_BranchId",
                table: "results");

            migrationBuilder.DropForeignKey(
                name: "FK_results_clinics_ClinicId",
                table: "results");

            migrationBuilder.DropIndex(
                name: "IX_results_BranchId",
                table: "results");

            migrationBuilder.DropIndex(
                name: "IX_results_ClinicId",
                table: "results");

            migrationBuilder.DropIndex(
                name: "IX_result_values_BranchId",
                table: "result_values");

            migrationBuilder.DropIndex(
                name: "IX_result_values_ClinicId",
                table: "result_values");

            migrationBuilder.DropIndex(
                name: "IX_registrations_BranchId",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_ClinicId",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registration_items_BranchId",
                table: "registration_items");

            migrationBuilder.DropIndex(
                name: "IX_registration_items_ClinicId",
                table: "registration_items");

            migrationBuilder.DropIndex(
                name: "IX_payments_BranchId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_ClinicId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_client_rates_BranchId",
                table: "client_rates");

            migrationBuilder.DropIndex(
                name: "IX_client_rates_ClinicId",
                table: "client_rates");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BranchId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "results");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "results");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "client_rates");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "client_rates");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
