using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VetLims.Infrastructure;

#nullable disable

namespace VetLims.Infrastructure.Migrations;

/// <summary>
/// Corrective prerequisite for the legacy migration history: animal reference masters
/// exist in the EF model but were never introduced by a historical migration.
/// </summary>
[DbContext(typeof(VetLimsDbContext))]
[Migration("20260802121000_AnimalReferenceMasterFoundation")]
public partial class AnimalReferenceMasterFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateBasicCatalog(migrationBuilder, "breeds", 200);
        CreateBasicCatalog(migrationBuilder, "animal_colors", 200);
        CreateBasicCatalog(migrationBuilder, "genders", 100);
        CreateDoctorCatalog(migrationBuilder);
        CreateBasicCatalog(migrationBuilder, "sample_types", 200);
        CreateBasicCatalog(migrationBuilder, "containers", 200);
        CreateBasicCatalog(migrationBuilder, "methods", 200);
        CreateInstrumentCatalog(migrationBuilder);
        CreateSupplierCatalog(migrationBuilder);
        CreateMedicineCatalog(migrationBuilder);
        CreateBasicCatalog(migrationBuilder, "inventory_categories", 200);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inventory_categories");
        migrationBuilder.DropTable(name: "medicines");
        migrationBuilder.DropTable(name: "suppliers");
        migrationBuilder.DropTable(name: "instruments");
        migrationBuilder.DropTable(name: "methods");
        migrationBuilder.DropTable(name: "containers");
        migrationBuilder.DropTable(name: "sample_types");
        migrationBuilder.DropTable(name: "doctors");
        migrationBuilder.DropTable(name: "genders");
        migrationBuilder.DropTable(name: "animal_colors");
        migrationBuilder.DropTable(name: "breeds");
    }

    private static void CreateBasicCatalog(MigrationBuilder migrationBuilder, string tableName, int nameLength)
    {
        migrationBuilder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: $"character varying({nameLength})", maxLength: nameLength, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey($"PK_{tableName}", x => x.Id));

        migrationBuilder.CreateIndex(name: $"IX_{tableName}_Code", table: tableName, column: "Code", unique: true);
    }

    private static void CreateDoctorCatalog(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "doctors",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DepartmentId = table.Column<int>(type: "integer", nullable: true),
                Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_doctors", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_doctors_Code", table: "doctors", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_doctors_DepartmentId", table: "doctors", column: "DepartmentId");
    }

    private static void CreateInstrumentCatalog(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "instruments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_instruments", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_instruments_Code", table: "instruments", column: "Code", unique: true);
    }

    private static void CreateSupplierCatalog(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "suppliers",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Address = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_suppliers", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_suppliers_Code", table: "suppliers", column: "Code", unique: true);
    }

    private static void CreateMedicineCatalog(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "medicines",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                GenericName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ModifiedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_medicines", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_medicines_Code", table: "medicines", column: "Code", unique: true);
    }
}
