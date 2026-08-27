using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUrnBillOfMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrnasListaMateriales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrnasListaMateriales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrnasListaMateriales_Urnas_UrnId",
                        column: x => x.UrnId,
                        principalTable: "Urnas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UrnasListaMaterialesPartidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnBillOfMaterialsId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrnasListaMaterialesPartidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrnasListaMaterialesPartidas_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrnasListaMaterialesPartidas_UrnasListaMateriales_UrnBillOf~",
                        column: x => x.UrnBillOfMaterialsId,
                        principalTable: "UrnasListaMateriales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrnasListaMateriales_UrnId",
                table: "UrnasListaMateriales",
                column: "UrnId",
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UrnasListaMateriales_UrnId_Version",
                table: "UrnasListaMateriales",
                columns: new[] { "UrnId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrnasListaMaterialesPartidas_SupplyItemId",
                table: "UrnasListaMaterialesPartidas",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UrnasListaMaterialesPartidas_UrnBillOfMaterialsId",
                table: "UrnasListaMaterialesPartidas",
                column: "UrnBillOfMaterialsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrnasListaMaterialesPartidas");

            migrationBuilder.DropTable(
                name: "UrnasListaMateriales");
        }
    }
}
