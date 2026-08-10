using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations;

/// <summary>
/// Step 6: Add durable owner/patient and medical-history tables only.
/// Existing master tables are deliberately not recreated: the historical EF snapshot predates them.
/// </summary>
public partial class OwnerAndAnimalManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "owners",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Mobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                WhatsApp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Address = table.Column<string>(type: "text", nullable: true),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
            constraints: table => table.PrimaryKey("PK_owners", x => x.Id));

        migrationBuilder.CreateTable(
            name: "animals",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                OwnerId = table.Column<int>(type: "integer", nullable: false), SpeciesId = table.Column<int>(type: "integer", nullable: false), BreedId = table.Column<int>(type: "integer", nullable: true), GenderId = table.Column<int>(type: "integer", nullable: true), ColorId = table.Column<int>(type: "integer", nullable: true),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true), WeightKg = table.Column<decimal>(type: "numeric(8,3)", nullable: true), MicrochipNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true), PhotoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), VaccinationStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true), IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), CreatedBy = table.Column<string>(type: "text", nullable: true), ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), ModifiedBy = table.Column<string>(type: "text", nullable: true), IsDeleted = table.Column<bool>(type: "boolean", nullable: false), DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_animals", x => x.Id);
                table.ForeignKey("FK_animals_owners_OwnerId", x => x.OwnerId, "owners", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_animals_species_SpeciesId", x => x.SpeciesId, "species", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_animals_breeds_BreedId", x => x.BreedId, "breeds", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_animals_genders_GenderId", x => x.GenderId, "genders", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_animals_animal_colors_ColorId", x => x.ColorId, "animal_colors", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "medical_history_entries",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), AnimalId = table.Column<int>(type: "integer", nullable: false), OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), EntryType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false), Diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), Treatment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true), Medication = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), Notes = table.Column<string>(type: "text", nullable: true), CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), CreatedBy = table.Column<string>(type: "text", nullable: true), ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), ModifiedBy = table.Column<string>(type: "text", nullable: true), IsDeleted = table.Column<bool>(type: "boolean", nullable: false), DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_medical_history_entries", x => x.Id); table.ForeignKey("FK_medical_history_entries_animals_AnimalId", x => x.AnimalId, "animals", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateIndex(name: "IX_owners_Code", table: "owners", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_owners_Name", table: "owners", column: "Name");
        migrationBuilder.CreateIndex(name: "IX_owners_Mobile", table: "owners", column: "Mobile");
        migrationBuilder.CreateIndex(name: "IX_animals_Code", table: "animals", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_animals_MicrochipNumber", table: "animals", column: "MicrochipNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_animals_OwnerId_Name", table: "animals", columns: new[] { "OwnerId", "Name" });
        migrationBuilder.CreateIndex(name: "IX_animals_SpeciesId", table: "animals", column: "SpeciesId");
        migrationBuilder.CreateIndex(name: "IX_animals_BreedId", table: "animals", column: "BreedId");
        migrationBuilder.CreateIndex(name: "IX_animals_GenderId", table: "animals", column: "GenderId");
        migrationBuilder.CreateIndex(name: "IX_animals_ColorId", table: "animals", column: "ColorId");
        migrationBuilder.CreateIndex(name: "IX_medical_history_entries_AnimalId_OccurredAt", table: "medical_history_entries", columns: new[] { "AnimalId", "OccurredAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "medical_history_entries");
        migrationBuilder.DropTable(name: "animals");
        migrationBuilder.DropTable(name: "owners");
    }
}
