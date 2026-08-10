using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LaboratoryWorkflowEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigitalSignature",
                table: "results",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PathologistAuthenticatedAt",
                table: "results",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PathologistAuthenticatedBy",
                table: "results",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TechnicianVerifiedAt",
                table: "results",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianVerifiedBy",
                table: "results",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRemarks",
                table: "results",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntryType",
                table: "result_values",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FormulaExpression",
                table: "result_values",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaResult",
                table: "result_values",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "result_values",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfAttachmentUrl",
                table: "result_values",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositiveNegativeValue",
                table: "result_values",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClinicalNotes",
                table: "registrations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoctorId",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleCollectedAt",
                table: "registration_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleNotes",
                table: "registration_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleProcessedAt",
                table: "registration_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleReceivedAt",
                table: "registration_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SampleStatus",
                table: "registration_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SampleTypeId",
                table: "registration_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnimalId",
                table: "registration_animals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_DoctorId",
                table: "registrations",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_OwnerId",
                table: "registrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_items_SampleTypeId",
                table: "registration_items",
                column: "SampleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_animals_AnimalId",
                table: "registration_animals",
                column: "AnimalId");

            migrationBuilder.AddForeignKey(
                name: "FK_registration_animals_animals_AnimalId",
                table: "registration_animals",
                column: "AnimalId",
                principalTable: "animals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registration_items_sample_types_SampleTypeId",
                table: "registration_items",
                column: "SampleTypeId",
                principalTable: "sample_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_doctors_DoctorId",
                table: "registrations",
                column: "DoctorId",
                principalTable: "doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registrations_owners_OwnerId",
                table: "registrations",
                column: "OwnerId",
                principalTable: "owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registration_animals_animals_AnimalId",
                table: "registration_animals");

            migrationBuilder.DropForeignKey(
                name: "FK_registration_items_sample_types_SampleTypeId",
                table: "registration_items");

            migrationBuilder.DropForeignKey(
                name: "FK_registrations_doctors_DoctorId",
                table: "registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_registrations_owners_OwnerId",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_DoctorId",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registrations_OwnerId",
                table: "registrations");

            migrationBuilder.DropIndex(
                name: "IX_registration_items_SampleTypeId",
                table: "registration_items");

            migrationBuilder.DropIndex(
                name: "IX_registration_animals_AnimalId",
                table: "registration_animals");

            migrationBuilder.DropColumn(
                name: "DigitalSignature",
                table: "results");

            migrationBuilder.DropColumn(
                name: "PathologistAuthenticatedAt",
                table: "results");

            migrationBuilder.DropColumn(
                name: "PathologistAuthenticatedBy",
                table: "results");

            migrationBuilder.DropColumn(
                name: "TechnicianVerifiedAt",
                table: "results");

            migrationBuilder.DropColumn(
                name: "TechnicianVerifiedBy",
                table: "results");

            migrationBuilder.DropColumn(
                name: "VerificationRemarks",
                table: "results");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "FormulaExpression",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "FormulaResult",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "PdfAttachmentUrl",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "PositiveNegativeValue",
                table: "result_values");

            migrationBuilder.DropColumn(
                name: "ClinicalNotes",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "SampleCollectedAt",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "SampleNotes",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "SampleProcessedAt",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "SampleReceivedAt",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "SampleStatus",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "SampleTypeId",
                table: "registration_items");

            migrationBuilder.DropColumn(
                name: "AnimalId",
                table: "registration_animals");
        }
    }
}
