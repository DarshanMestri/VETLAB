using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLocationIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The column does not exist in databases created before this change,
            // so this must be an AddColumn (an ALTER would fail with 42703).
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "user_locations",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "user_locations");
        }
    }
}
