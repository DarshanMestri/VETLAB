using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgeUnit",
                table: "registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgeValue",
                table: "registrations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeUnit",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "AgeValue",
                table: "registrations");
        }
    }
}
