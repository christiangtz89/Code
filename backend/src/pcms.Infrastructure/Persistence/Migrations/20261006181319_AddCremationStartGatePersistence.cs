using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationStartGatePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagoRequeridoInicio",
                table: "Cremaciones",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VerificacionesInicioCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoQrRecepcionSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfirmadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarioConfirmoSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacionesInicioCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificacionesInicioCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificacionesInicioCremacion_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificacionesInicioCremacion_Usuarios_ConfirmadoPorUsuario~",
                        column: x => x.ConfirmadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesInicioCremacion_ConfirmadoPorUsuarioId",
                table: "VerificacionesInicioCremacion",
                column: "ConfirmadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesInicioCremacion_CremacionId",
                table: "VerificacionesInicioCremacion",
                column: "CremacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesInicioCremacion_RecepcionId",
                table: "VerificacionesInicioCremacion",
                column: "RecepcionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificacionesInicioCremacion");

            migrationBuilder.DropColumn(
                name: "MontoPagoRequeridoInicio",
                table: "Cremaciones");
        }
    }
}
