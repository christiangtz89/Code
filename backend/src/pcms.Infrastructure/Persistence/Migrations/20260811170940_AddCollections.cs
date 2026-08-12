using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecoleccionId",
                table: "Recepciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Recolecciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MascotaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecolectadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoUbicacion = table.Column<int>(type: "integer", nullable: false),
                    VeterinariaId = table.Column<Guid>(type: "uuid", nullable: true),
                    VeterinarioReferenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    CodigoQr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DireccionRecoleccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NombreContactoRecoleccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TelefonoContactoRecoleccion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PesoAproximadoKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    TieneObjetosPersonales = table.Column<bool>(type: "boolean", nullable: false),
                    DescripcionObjetosPersonales = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaRecoleccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRecepcionInstalaciones = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recolecciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recolecciones_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recolecciones_Usuarios_RecolectadoPorUsuarioId",
                        column: x => x.RecolectadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recolecciones_Veterinarias_VeterinariaId",
                        column: x => x.VeterinariaId,
                        principalTable: "Veterinarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recolecciones_Veterinarios_VeterinarioReferenteId",
                        column: x => x.VeterinarioReferenteId,
                        principalTable: "Veterinarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_RecoleccionId",
                table: "Recepciones",
                column: "RecoleccionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_CodigoQr",
                table: "Recolecciones",
                column: "CodigoQr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_Estado",
                table: "Recolecciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_FechaRecoleccion",
                table: "Recolecciones",
                column: "FechaRecoleccion");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_MascotaId",
                table: "Recolecciones",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_RecolectadoPorUsuarioId",
                table: "Recolecciones",
                column: "RecolectadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_VeterinariaId",
                table: "Recolecciones",
                column: "VeterinariaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_VeterinarioReferenteId",
                table: "Recolecciones",
                column: "VeterinarioReferenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recepciones_Recolecciones_RecoleccionId",
                table: "Recepciones",
                column: "RecoleccionId",
                principalTable: "Recolecciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recepciones_Recolecciones_RecoleccionId",
                table: "Recepciones");

            migrationBuilder.DropTable(
                name: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recepciones_RecoleccionId",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "RecoleccionId",
                table: "Recepciones");
        }
    }
}
