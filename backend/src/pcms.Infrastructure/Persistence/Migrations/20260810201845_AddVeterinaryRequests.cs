using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVeterinaryRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesVeterinarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VeterinariaId = table.Column<Guid>(type: "uuid", nullable: true),
                    VeterinarioReferenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecepcionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    NombrePropietario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApellidoPaternoPropietario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApellidoMaternoPropietario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TelefonoPropietario = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CorreoPropietario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NombreMascota = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Especie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Raza = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sexo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PesoAproximadoKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EdadAnios = table.Column<int>(type: "integer", nullable: true),
                    FechaFallecimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipoCremacionSolicitado = table.Column<int>(type: "integer", nullable: true),
                    NombrePaqueteSolicitado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NotasSolicitud = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NotasInternas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaConversion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesVeterinarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesVeterinarias_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVeterinarias_Usuarios_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVeterinarias_Usuarios_RevisadoPorUsuarioId",
                        column: x => x.RevisadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVeterinarias_Veterinarias_VeterinariaId",
                        column: x => x.VeterinariaId,
                        principalTable: "Veterinarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVeterinarias_Veterinarios_VeterinarioReferenteId",
                        column: x => x.VeterinarioReferenteId,
                        principalTable: "Veterinarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_Estado",
                table: "SolicitudesVeterinarias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_FechaSolicitud",
                table: "SolicitudesVeterinarias",
                column: "FechaSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_RecepcionId",
                table: "SolicitudesVeterinarias",
                column: "RecepcionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_RegistradoPorUsuarioId",
                table: "SolicitudesVeterinarias",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_RevisadoPorUsuarioId",
                table: "SolicitudesVeterinarias",
                column: "RevisadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_VeterinariaId",
                table: "SolicitudesVeterinarias",
                column: "VeterinariaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVeterinarias_VeterinarioReferenteId",
                table: "SolicitudesVeterinarias",
                column: "VeterinarioReferenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesVeterinarias");
        }
    }
}
