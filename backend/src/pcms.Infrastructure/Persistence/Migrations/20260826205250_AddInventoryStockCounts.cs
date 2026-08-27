using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryStockCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConteosInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyInventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    SystemQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    CountedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CountedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConteosInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConteosInventario_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteosInventario_LotesInventarioInsumos_SupplyInventoryLot~",
                        column: x => x.SupplyInventoryLotId,
                        principalTable: "LotesInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteosInventario_MovimientosInventarioInsumos_InventoryMov~",
                        column: x => x.InventoryMovementId,
                        principalTable: "MovimientosInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConteosInventario_Usuarios_CountedByUserId",
                        column: x => x.CountedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntregasInventarioCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CremationUrnReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnMovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FulfilledByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasInventarioCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasInventarioCremacion_Cremaciones_CremationId",
                        column: x => x.CremationId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregasInventarioCremacion_MovimientosInventarioInsumos_Ur~",
                        column: x => x.UrnMovementId,
                        principalTable: "MovimientosInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregasInventarioCremacion_Usuarios_FulfilledByUserId",
                        column: x => x.FulfilledByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesEntregaCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfillmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplyItemNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UnitOfMeasureSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesEntregaCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialesEntregaCremacion_EntregasInventarioCremacion_Fulf~",
                        column: x => x.FulfillmentId,
                        principalTable: "EntregasInventarioCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialesEntregaCremacion_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialesEntregaCremacion_LotesInventarioInsumos_LotId",
                        column: x => x.LotId,
                        principalTable: "LotesInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialesEntregaCremacion_MovimientosInventarioInsumos_Inv~",
                        column: x => x.InventoryMovementId,
                        principalTable: "MovimientosInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservasUrnaCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SupplyItemNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SupplyItemScanCodeSnapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReservedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FulfilledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FulfillmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasUrnaCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Cremaciones_CremationId",
                        column: x => x.CremationId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_EntregasInventarioCremacion_Fulfillme~",
                        column: x => x.FulfillmentId,
                        principalTable: "EntregasInventarioCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Urnas_UrnId",
                        column: x => x.UrnId,
                        principalTable: "Urnas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Usuarios_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Usuarios_FulfilledByUserId",
                        column: x => x.FulfilledByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReservasUrnaCremacion_Usuarios_ReservedByUserId",
                        column: x => x.ReservedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConteosInventario_CountedByUserId",
                table: "ConteosInventario",
                column: "CountedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosInventario_InventoryMovementId",
                table: "ConteosInventario",
                column: "InventoryMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosInventario_SupplyInventoryLotId",
                table: "ConteosInventario",
                column: "SupplyInventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConteosInventario_SupplyItemId_CountedAt",
                table: "ConteosInventario",
                columns: new[] { "SupplyItemId", "CountedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntregasInventarioCremacion_CremationId",
                table: "EntregasInventarioCremacion",
                column: "CremationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregasInventarioCremacion_FulfilledByUserId",
                table: "EntregasInventarioCremacion",
                column: "FulfilledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregasInventarioCremacion_UrnMovementId",
                table: "EntregasInventarioCremacion",
                column: "UrnMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesEntregaCremacion_FulfillmentId",
                table: "MaterialesEntregaCremacion",
                column: "FulfillmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesEntregaCremacion_InventoryMovementId",
                table: "MaterialesEntregaCremacion",
                column: "InventoryMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesEntregaCremacion_LotId",
                table: "MaterialesEntregaCremacion",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesEntregaCremacion_SupplyItemId",
                table: "MaterialesEntregaCremacion",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_CancelledByUserId",
                table: "ReservasUrnaCremacion",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_CremationId_Status",
                table: "ReservasUrnaCremacion",
                columns: new[] { "CremationId", "Status" },
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_FulfilledByUserId",
                table: "ReservasUrnaCremacion",
                column: "FulfilledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_FulfillmentId",
                table: "ReservasUrnaCremacion",
                column: "FulfillmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_ReservedByUserId",
                table: "ReservasUrnaCremacion",
                column: "ReservedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_SupplyItemId",
                table: "ReservasUrnaCremacion",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservasUrnaCremacion_UrnId",
                table: "ReservasUrnaCremacion",
                column: "UrnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConteosInventario");

            migrationBuilder.DropTable(
                name: "MaterialesEntregaCremacion");

            migrationBuilder.DropTable(
                name: "ReservasUrnaCremacion");

            migrationBuilder.DropTable(
                name: "EntregasInventarioCremacion");
        }
    }
}
