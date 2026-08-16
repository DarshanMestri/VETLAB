using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    [DbContext(typeof(VetLimsDbContext))]
    [Migration("20260810185200_MultiLabPhase1")]
    public partial class MultiLabPhase1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Organizations: new columns
            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipOrPin",
                table: "organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Clinics (Laboratory Master): new columns
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "clinics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "clinics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "clinics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSettingsJson",
                table: "clinics",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "clinics",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "clinics",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Branches (Location): new columns
            migrationBuilder.AddColumn<int>(
                name: "LocationType",
                table: "branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "branches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "branches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manager",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // These objects may exist in legacy databases whose migration history omitted
            // MultiLabPhase1. Keep this section replayable while preserving normal clean setup.
            migrationBuilder.Sql(
                """
                ALTER TABLE "AspNetUsers"
                    ADD COLUMN IF NOT EXISTS "OrganizationId" integer;

                CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_OrganizationId"
                    ON "AspNetUsers" ("OrganizationId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_AspNetUsers_organizations_OrganizationId'
                          AND conrelid = '"AspNetUsers"'::regclass
                    ) THEN
                        ALTER TABLE "AspNetUsers"
                            ADD CONSTRAINT "FK_AspNetUsers_organizations_OrganizationId"
                            FOREIGN KEY ("OrganizationId") REFERENCES organizations ("Id")
                            ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS user_locations (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
                    "UserId" integer NOT NULL,
                    "BranchId" integer NOT NULL,
                    "IsDefault" boolean NOT NULL DEFAULT FALSE,
                    "CreatedDate" timestamp with time zone NOT NULL,
                    "CreatedBy" text NULL,
                    "ModifiedDate" timestamp with time zone NULL,
                    "ModifiedBy" text NULL,
                    "IsDeleted" boolean NOT NULL,
                    "DeletedDate" timestamp with time zone NULL,
                    CONSTRAINT "PK_user_locations" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_user_locations_AspNetUsers_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_user_locations_branches_BranchId"
                        FOREIGN KEY ("BranchId") REFERENCES branches ("Id") ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_locations_UserId_BranchId"
                    ON user_locations ("UserId", "BranchId");
                CREATE INDEX IF NOT EXISTS "IX_user_locations_UserId"
                    ON user_locations ("UserId");
                CREATE INDEX IF NOT EXISTS "IX_user_locations_BranchId"
                    ON user_locations ("BranchId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_locations");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "State",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ZipOrPin",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "EmailSettingsJson",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "clinics");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "City",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "State",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Pin",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Manager",
                table: "branches");
        }
    }
}
