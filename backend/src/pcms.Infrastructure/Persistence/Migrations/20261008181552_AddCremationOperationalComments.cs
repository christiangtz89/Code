using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationOperationalComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComentariosOperativosCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarioSnapshot = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosOperativosCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComentariosOperativosCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComentariosOperativosCremacion_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosOperativosCremacion_CreadoPorUsuarioId",
                table: "ComentariosOperativosCremacion",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosOperativosCremacion_CremacionId_FechaCreacion",
                table: "ComentariosOperativosCremacion",
                columns: new[] { "CremacionId", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComentariosOperativosCremacion");
        }
    }
}
