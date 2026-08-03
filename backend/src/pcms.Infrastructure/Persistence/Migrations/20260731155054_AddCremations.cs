using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cremaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsignadoAUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    TipoCremacion = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    NombrePaquete = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IncluyeUrna = table.Column<bool>(type: "boolean", nullable: false),
                    DescripcionUrna = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IncluyeHuella = table.Column<bool>(type: "boolean", nullable: false),
                    IncluyeCertificado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaFinalizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaListaParaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstruccionesEspeciales = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cremaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cremaciones_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cremaciones_Usuarios_AsignadoAUsuarioId",
                        column: x => x.AsignadoAUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_AsignadoAUsuarioId",
                table: "Cremaciones",
                column: "AsignadoAUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_Estado",
                table: "Cremaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_FechaProgramada",
                table: "Cremaciones",
                column: "FechaProgramada");

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_RecepcionId",
                table: "Cremaciones",
                column: "RecepcionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cremaciones");
        }
    }
}
