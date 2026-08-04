using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSecondLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "Clientes",
                newName: "ApellidoPaterno");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMaterno",
                table: "Clientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApellidoMaterno",
                table: "Clientes");

            migrationBuilder.RenameColumn(
                name: "ApellidoPaterno",
                table: "Clientes",
                newName: "Apellido");
        }
    }
}
