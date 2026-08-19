using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnimalBarcodesAndPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "registration_animals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_registration_animals_Barcode",
                table: "registration_animals",
                column: "Barcode",
                unique: true,
                filter: "\"Barcode\" IS NOT NULL");

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

            migrationBuilder.DropIndex(
                name: "IX_registration_animals_Barcode",
                table: "registration_animals");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "registration_animals");
        }
    }
}
