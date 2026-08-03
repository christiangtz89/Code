using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVeterinaryClinicsAndVeterinarians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mascotas_Clientes_CustomerId",
                table: "Mascotas");

            migrationBuilder.CreateTable(
    name: "Veterinarias",
    columns: table => new
    {
        Id = table.Column<Guid>(type: "uuid", nullable: false),
        Nombre = table.Column<string>(
            type: "character varying(150)",
            maxLength: 150,
            nullable: false),
        Telefono = table.Column<string>(
            type: "character varying(30)",
            maxLength: 30,
            nullable: true),
        CorreoElectronico = table.Column<string>(
            type: "character varying(150)",
            maxLength: 150,
            nullable: true),
        Direccion = table.Column<string>(
            type: "character varying(300)",
            maxLength: 300,
            nullable: true),
        NombreContactoPrincipal = table.Column<string>(
            type: "character varying(150)",
            maxLength: 150,
            nullable: true),
        Activo = table.Column<bool>(
            type: "boolean",
            nullable: false),
        FechaCreacion = table.Column<DateTime>(
            type: "timestamp with time zone",
            nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Veterinarias", x => x.Id);
    });

            migrationBuilder.CreateTable(
                name: "Veterinarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VeterinariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CedulaProfesional = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veterinarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veterinarios_Veterinarias_VeterinariaId",
                        column: x => x.VeterinariaId,
                        principalTable: "Veterinarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarias_CorreoElectronico",
                table: "Veterinarias",
                column: "CorreoElectronico");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarias_Nombre",
                table: "Veterinarias",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarios_CedulaProfesional",
                table: "Veterinarios",
                column: "CedulaProfesional");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarios_CorreoElectronico",
                table: "Veterinarios",
                column: "CorreoElectronico");

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarios_VeterinariaId",
                table: "Veterinarios",
                column: "VeterinariaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mascotas_Clientes_CustomerId",
                table: "Mascotas",
                column: "CustomerId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mascotas_Clientes_CustomerId",
                table: "Mascotas");

            migrationBuilder.DropTable(
                name: "Veterinarios");

            migrationBuilder.DropTable(
                name: "Veterinarias");

            migrationBuilder.AddForeignKey(
                name: "FK_Mascotas_Clientes_CustomerId",
                table: "Mascotas",
                column: "CustomerId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
