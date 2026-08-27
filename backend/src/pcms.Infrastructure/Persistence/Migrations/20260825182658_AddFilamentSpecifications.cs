using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFilamentSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EspecificacionesFilamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Color = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NetUsableWeightGrams = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ManufacturerProductCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ProductData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EspecificacionesFilamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EspecificacionesFilamento_Insumos_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EspecificacionesFilamento_SupplyItemId",
                table: "EspecificacionesFilamento",
                column: "SupplyItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EspecificacionesFilamento");
        }
    }
}
