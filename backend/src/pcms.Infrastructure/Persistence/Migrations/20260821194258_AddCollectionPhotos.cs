using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosRecoleccion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecoleccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubidoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoFoto = table.Column<int>(type: "integer", nullable: false),
                    NombreArchivoOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NombreArchivoGuardado = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RutaAlmacenamiento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosRecoleccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosRecoleccion_Recolecciones_RecoleccionId",
                        column: x => x.RecoleccionId,
                        principalTable: "Recolecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FotosRecoleccion_Usuarios_SubidoPorUsuarioId",
                        column: x => x.SubidoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosRecoleccion_NombreArchivoGuardado",
                table: "FotosRecoleccion",
                column: "NombreArchivoGuardado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FotosRecoleccion_RecoleccionId",
                table: "FotosRecoleccion",
                column: "RecoleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_FotosRecoleccion_SubidoPorUsuarioId",
                table: "FotosRecoleccion",
                column: "SubidoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosRecoleccion");
        }
    }
}
