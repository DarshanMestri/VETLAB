using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetLims.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryPharmacyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    InvoiceReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_purchases_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_purchase_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryPurchaseId = table.Column<int>(type: "integer", nullable: false),
                    MedicineId = table.Column<int>(type: "integer", nullable: false),
                    InventoryCategoryId = table.Column<int>(type: "integer", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LowStockThreshold = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_purchase_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_lines_inventory_categories_InventoryCate~",
                        column: x => x.InventoryCategoryId,
                        principalTable: "inventory_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_lines_inventory_purchases_InventoryPurch~",
                        column: x => x.InventoryPurchaseId,
                        principalTable: "inventory_purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_lines_medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_purchase_returns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReturnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InventoryPurchaseId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalReturnAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_purchase_returns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_returns_inventory_purchases_InventoryPur~",
                        column: x => x.InventoryPurchaseId,
                        principalTable: "inventory_purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_consumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryPurchaseLineId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_consumptions_inventory_purchase_lines_InventoryPu~",
                        column: x => x.InventoryPurchaseLineId,
                        principalTable: "inventory_purchase_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_purchase_return_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryPurchaseReturnId = table.Column<int>(type: "integer", nullable: false),
                    InventoryPurchaseLineId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_purchase_return_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_return_lines_inventory_purchase_lines_In~",
                        column: x => x.InventoryPurchaseLineId,
                        principalTable: "inventory_purchase_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_purchase_return_lines_inventory_purchase_returns_~",
                        column: x => x.InventoryPurchaseReturnId,
                        principalTable: "inventory_purchase_returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_consumptions_ConsumedAt",
                table: "inventory_consumptions",
                column: "ConsumedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_consumptions_InventoryPurchaseLineId",
                table: "inventory_consumptions",
                column: "InventoryPurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_lines_ExpiryDate",
                table: "inventory_purchase_lines",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_lines_InventoryCategoryId",
                table: "inventory_purchase_lines",
                column: "InventoryCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_lines_InventoryPurchaseId",
                table: "inventory_purchase_lines",
                column: "InventoryPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_lines_MedicineId_BatchNumber",
                table: "inventory_purchase_lines",
                columns: new[] { "MedicineId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_return_lines_InventoryPurchaseLineId",
                table: "inventory_purchase_return_lines",
                column: "InventoryPurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_return_lines_InventoryPurchaseReturnId",
                table: "inventory_purchase_return_lines",
                column: "InventoryPurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_returns_InventoryPurchaseId",
                table: "inventory_purchase_returns",
                column: "InventoryPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_returns_ReturnDate",
                table: "inventory_purchase_returns",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchase_returns_ReturnNumber",
                table: "inventory_purchase_returns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchases_PurchaseDate",
                table: "inventory_purchases",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchases_PurchaseNumber",
                table: "inventory_purchases",
                column: "PurchaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_purchases_SupplierId",
                table: "inventory_purchases",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_consumptions");

            migrationBuilder.DropTable(
                name: "inventory_purchase_return_lines");

            migrationBuilder.DropTable(
                name: "inventory_purchase_lines");

            migrationBuilder.DropTable(
                name: "inventory_purchase_returns");

            migrationBuilder.DropTable(
                name: "inventory_purchases");
        }
    }
}
