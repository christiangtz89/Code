using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasingAndSupplierCostHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceReference = table.Column<string>(type: "text", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Compras_Usuarios_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProveedoresInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierSku = table.Column<string>(type: "text", nullable: true),
                    CurrentUnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    PurchaseUnit = table.Column<string>(type: "text", nullable: false),
                    InventoryUnitsPerPurchaseUnit = table.Column<decimal>(type: "numeric(14,6)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedoresInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProveedoresInsumos_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProveedoresInsumos_Proveedores_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialCostosProveedorInsumo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierSupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialCostosProveedorInsumo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialCostosProveedorInsumo_ProveedoresInsumos_SupplierS~",
                        column: x => x.SupplierSupplyItemId,
                        principalTable: "ProveedoresInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartidasCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierSupplyItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    DescriptionSnapshot = table.Column<string>(type: "text", nullable: false),
                    SupplierSkuSnapshot = table.Column<string>(type: "text", nullable: true),
                    PurchaseUnitSnapshot = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    LineSubtotal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    NormalizedReceivedQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasCompra_Compras_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartidasCompra_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartidasCompra_ProveedoresInsumos_SupplierSupplyItemId",
                        column: x => x.SupplierSupplyItemId,
                        principalTable: "ProveedoresInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_RecordedByUserId",
                table: "Compras",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_SupplierId",
                table: "Compras",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCostosProveedorInsumo_SupplierSupplyItemId_Effecti~",
                table: "HistorialCostosProveedorInsumo",
                columns: new[] { "SupplierSupplyItemId", "EffectiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PartidasCompra_PurchaseId",
                table: "PartidasCompra",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasCompra_SupplierSupplyItemId",
                table: "PartidasCompra",
                column: "SupplierSupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasCompra_SupplyItemId",
                table: "PartidasCompra",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProveedoresInsumos_SupplierId_SupplyItemId",
                table: "ProveedoresInsumos",
                columns: new[] { "SupplierId", "SupplyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProveedoresInsumos_SupplyItemId",
                table: "ProveedoresInsumos",
                column: "SupplyItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialCostosProveedorInsumo");

            migrationBuilder.DropTable(
                name: "PartidasCompra");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropTable(
                name: "ProveedoresInsumos");
        }
    }
}
