using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.CreateTable(
    name: "Recepciones",
    columns: table => new
    {
        Id = table.Column<Guid>(type: "uuid", nullable: false),
        MascotaId = table.Column<Guid>(type: "uuid", nullable: false),
        RecibidoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
        FechaRecepcion = table.Column<DateTime>(
            type: "timestamp with time zone",
            nullable: false),
        CodigoQr = table.Column<string>(
            type: "character varying(100)",
            maxLength: 100,
            nullable: false),
        PesoVerificadoKg = table.Column<decimal>(
            type: "numeric(10,2)",
            precision: 10,
            scale: 2,
            nullable: false),
        TieneObjetosPersonales = table.Column<bool>(
            type: "boolean",
            nullable: false),
        DescripcionObjetosPersonales = table.Column<string>(
            type: "character varying(500)",
            maxLength: 500,
            nullable: true),
        Notas = table.Column<string>(
            type: "character varying(1000)",
            maxLength: 1000,
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
                    table.PrimaryKey("PK_Recepciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recepciones_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recepciones_Usuarios_RecibidoPorUsuarioId",
                        column: x => x.RecibidoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_CodigoQr",
                table: "Recepciones",
                column: "CodigoQr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_MascotaId",
                table: "Recepciones",
                column: "MascotaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_RecibidoPorUsuarioId",
                table: "Recepciones",
                column: "RecibidoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recepciones");
        }
    }
}
