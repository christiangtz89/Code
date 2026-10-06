using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationStartPaymentRevalidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevalidacionesPagoInicioCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarioConfirmoSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrecioCremacionSeleccionadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrecioCotizadoAnterior = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    PrecioCotizadoNuevo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MontoPagoRequeridoInicioEstablecido = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevalidacionesPagoInicioCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevalidacionesPagoInicioCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevalidacionesPagoInicioCremacion_PreciosCremacion_PrecioCr~",
                        column: x => x.PrecioCremacionSeleccionadoId,
                        principalTable: "PreciosCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevalidacionesPagoInicioCremacion_Usuarios_ConfirmadoPorUsu~",
                        column: x => x.ConfirmadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevalidacionesPagoInicioCremacion_ConfirmadoPorUsuarioId",
                table: "RevalidacionesPagoInicioCremacion",
                column: "ConfirmadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RevalidacionesPagoInicioCremacion_CremacionId",
                table: "RevalidacionesPagoInicioCremacion",
                column: "CremacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevalidacionesPagoInicioCremacion_PrecioCremacionSelecciona~",
                table: "RevalidacionesPagoInicioCremacion",
                column: "PrecioCremacionSeleccionadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevalidacionesPagoInicioCremacion");
        }
    }
}
