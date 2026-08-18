using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationQuoteSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PesoCotizadoKg",
                table: "Cremaciones",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMaximoCotizadoKg",
                table: "Cremaciones",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMinimoCotizadoKg",
                table: "Cremaciones",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCotizado",
                table: "Cremaciones",
                type: "numeric(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PesoCotizadoKg",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "PesoMaximoCotizadoKg",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "PesoMinimoCotizadoKg",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "PrecioCotizado",
                table: "Cremaciones");
        }
    }
}
