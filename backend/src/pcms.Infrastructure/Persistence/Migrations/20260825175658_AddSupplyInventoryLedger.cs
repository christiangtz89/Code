using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimientosInventarioInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PurchaseItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventarioInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosInventarioInsumos_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosInventarioInsumos_PartidasCompra_PurchaseItemId",
                        column: x => x.PurchaseItemId,
                        principalTable: "PartidasCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosInventarioInsumos_Usuarios_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_PurchaseItemId",
                table: "MovimientosInventarioInsumos",
                column: "PurchaseItemId",
                unique: true,
                filter: "\"PurchaseItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_RecordedByUserId",
                table: "MovimientosInventarioInsumos",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_SupplyItemId_OccurredAt",
                table: "MovimientosInventarioInsumos",
                columns: new[] { "SupplyItemId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosInventarioInsumos");
        }
    }
}
