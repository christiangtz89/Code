using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLotsAndPurchaseReceivingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseReceiptItemId",
                table: "MovimientosInventarioInsumos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplyInventoryLotId",
                table: "MovimientosInventarioInsumos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Compras",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Compras\" SET \"Status\" = 4 WHERE \"Status\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Compras",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "RecepcionesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepcionesCompra_Compras_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepcionesCompra_Usuarios_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartidasRecepcionCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    NormalizedReceivedQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    CurrencySnapshot = table.Column<string>(type: "text", nullable: false),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasRecepcionCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasRecepcionCompra_MovimientosInventarioInsumos_Invent~",
                        column: x => x.InventoryMovementId,
                        principalTable: "MovimientosInventarioInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartidasRecepcionCompra_PartidasCompra_PurchaseItemId",
                        column: x => x.PurchaseItemId,
                        principalTable: "PartidasCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartidasRecepcionCompra_RecepcionesCompra_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalTable: "RecepcionesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LotesInventarioInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ManufacturerLotNumber = table.Column<string>(type: "text", nullable: true),
                    PurchaseItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseReceiptItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesInventarioInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesInventarioInsumos_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesInventarioInsumos_PartidasCompra_PurchaseItemId",
                        column: x => x.PurchaseItemId,
                        principalTable: "PartidasCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesInventarioInsumos_PartidasRecepcionCompra_PurchaseRece~",
                        column: x => x.PurchaseReceiptItemId,
                        principalTable: "PartidasRecepcionCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_SupplyInventoryLotId",
                table: "MovimientosInventarioInsumos",
                column: "SupplyInventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesInventarioInsumos_PurchaseItemId",
                table: "LotesInventarioInsumos",
                column: "PurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesInventarioInsumos_PurchaseReceiptItemId",
                table: "LotesInventarioInsumos",
                column: "PurchaseReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesInventarioInsumos_ScanCode",
                table: "LotesInventarioInsumos",
                column: "ScanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotesInventarioInsumos_SupplyItemId",
                table: "LotesInventarioInsumos",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasRecepcionCompra_InventoryMovementId",
                table: "PartidasRecepcionCompra",
                column: "InventoryMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartidasRecepcionCompra_PurchaseItemId",
                table: "PartidasRecepcionCompra",
                column: "PurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasRecepcionCompra_PurchaseReceiptId",
                table: "PartidasRecepcionCompra",
                column: "PurchaseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCompra_PurchaseId_ReceivedAt",
                table: "RecepcionesCompra",
                columns: new[] { "PurchaseId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCompra_ReceivedByUserId",
                table: "RecepcionesCompra",
                column: "ReceivedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventarioInsumos_LotesInventarioInsumos_SupplyI~",
                table: "MovimientosInventarioInsumos",
                column: "SupplyInventoryLotId",
                principalTable: "LotesInventarioInsumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventarioInsumos_LotesInventarioInsumos_SupplyI~",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropTable(
                name: "LotesInventarioInsumos");

            migrationBuilder.DropTable(
                name: "PartidasRecepcionCompra");

            migrationBuilder.DropTable(
                name: "RecepcionesCompra");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_SupplyInventoryLotId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptItemId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "SupplyInventoryLotId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Compras");
        }
    }
}
