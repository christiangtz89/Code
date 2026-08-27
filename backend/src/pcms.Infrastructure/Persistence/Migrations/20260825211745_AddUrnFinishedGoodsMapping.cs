using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUrnFinishedGoodsMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinishedGoodsReceiptMovementId",
                table: "ProduccionesUrna",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UrnasInsumosTerminados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrnasInsumosTerminados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrnasInsumosTerminados_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrnasInsumosTerminados_Urnas_UrnId",
                        column: x => x.UrnId,
                        principalTable: "Urnas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProduccionesUrna_FinishedGoodsReceiptMovementId",
                table: "ProduccionesUrna",
                column: "FinishedGoodsReceiptMovementId",
                unique: true,
                filter: "\"FinishedGoodsReceiptMovementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UrnasInsumosTerminados_SupplyItemId",
                table: "UrnasInsumosTerminados",
                column: "SupplyItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrnasInsumosTerminados_UrnId",
                table: "UrnasInsumosTerminados",
                column: "UrnId",
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.AddForeignKey(
                name: "FK_ProduccionesUrna_MovimientosInventarioInsumos_FinishedGoods~",
                table: "ProduccionesUrna",
                column: "FinishedGoodsReceiptMovementId",
                principalTable: "MovimientosInventarioInsumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProduccionesUrna_MovimientosInventarioInsumos_FinishedGoods~",
                table: "ProduccionesUrna");

            migrationBuilder.DropTable(
                name: "UrnasInsumosTerminados");

            migrationBuilder.DropIndex(
                name: "IX_ProduccionesUrna_FinishedGoodsReceiptMovementId",
                table: "ProduccionesUrna");

            migrationBuilder.DropColumn(
                name: "FinishedGoodsReceiptMovementId",
                table: "ProduccionesUrna");
        }
    }
}
