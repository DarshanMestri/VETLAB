using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationDiscountAndTestSampleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SampleTypeId",
                table: "tests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "registrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "registrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "clinical_encounters",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "clinical_encounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "clinical_encounters",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_tests_SampleTypeId",
                table: "tests",
                column: "SampleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_tests_sample_types_SampleTypeId",
                table: "tests",
                column: "SampleTypeId",
                principalTable: "sample_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tests_sample_types_SampleTypeId",
                table: "tests");

            migrationBuilder.DropIndex(
                name: "IX_tests_SampleTypeId",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "SampleTypeId",
                table: "tests");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "clinical_encounters");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "clinical_encounters");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "clinical_encounters");
        }
    }
}
