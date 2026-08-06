using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVeterinarianOptionalClinicAndSecondLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Apellido",
                table: "Veterinarios",
                newName: "ApellidoPaterno");

            migrationBuilder.AlterColumn<Guid>(
                name: "VeterinariaId",
                table: "Veterinarios",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMaterno",
                table: "Veterinarios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApellidoMaterno",
                table: "Veterinarios");

            migrationBuilder.RenameColumn(
                name: "ApellidoPaterno",
                table: "Veterinarios",
                newName: "Apellido");

            migrationBuilder.AlterColumn<Guid>(
                name: "VeterinariaId",
                table: "Veterinarios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
