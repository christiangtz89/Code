using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturedUrnProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProduccionesUrna",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnBillOfMaterialsId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityProduced = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ProducedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProduccionesUrna", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProduccionesUrna_UrnasListaMateriales_UrnBillOfMaterialsId",
                        column: x => x.UrnBillOfMaterialsId,
                        principalTable: "UrnasListaMateriales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProduccionesUrna_Urnas_UrnId",
                        column: x => x.UrnId,
                        principalTable: "Urnas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProduccionesUrna_Usuarios_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumosMaterialProduccion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    WasteQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: false),
                    CostPerUnitSnapshot = table.Column<decimal>(type: "numeric(14,8)", nullable: true),
                    TotalMaterialCostSnapshot = table.Column<decimal>(type: "numeric(14,4)", nullable: true),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosMaterialProduccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterialProduccion_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterialProduccion_MovimientosInventarioInsumos_Inv~",
                        column: x => x.InventoryMovementId,
                        principalTable: "MovimientosInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumosMaterialProduccion_ProduccionesUrna_ProductionId",
                        column: x => x.ProductionId,
                        principalTable: "ProduccionesUrna",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterialProduccion_InventoryMovementId",
                table: "ConsumosMaterialProduccion",
                column: "InventoryMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterialProduccion_ProductionId",
                table: "ConsumosMaterialProduccion",
                column: "ProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosMaterialProduccion_SupplyItemId",
                table: "ConsumosMaterialProduccion",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesUrna_RecordedByUserId",
                table: "ProduccionesUrna",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesUrna_UrnBillOfMaterialsId",
                table: "ProduccionesUrna",
                column: "UrnBillOfMaterialsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesUrna_UrnId",
                table: "ProduccionesUrna",
                column: "UrnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosMaterialProduccion");

            migrationBuilder.DropTable(
                name: "ProduccionesUrna");
        }
    }
}
