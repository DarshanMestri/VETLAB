using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrectOrganizationOwnershipModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Metadata-only correction. The previous snapshot described schema objects that
            // were never created by the checked-in migration chain. The physical schema is
            // already correct, so applying this migration must not create, drop, or alter objects.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No schema operations are reversed because Up does not alter the database.
        }
    }
}
