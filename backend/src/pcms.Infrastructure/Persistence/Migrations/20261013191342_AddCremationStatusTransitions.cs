using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationStatusTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransicionesEstadoCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "integer", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "integer", nullable: false),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    FechaOcurrencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ComentarioOperativoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransicionesEstadoCremacion", x => x.Id);
                    table.CheckConstraint("CK_TransicionesEstadoCremacion_Cambio", "\"EstadoAnterior\" <> \"EstadoNuevo\"");
                    table.CheckConstraint("CK_TransicionesEstadoCremacion_Secuencia", "\"Secuencia\" > 0");
                    table.ForeignKey(
                        name: "FK_TransicionesEstadoCremacion_ComentariosOperativosCremacion_~",
                        column: x => x.ComentarioOperativoId,
                        principalTable: "ComentariosOperativosCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransicionesEstadoCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransicionesEstadoCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransicionesEstadoCremacion_ActorUsuarioId",
                table: "TransicionesEstadoCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TransicionesEstadoCremacion_ComentarioOperativoId",
                table: "TransicionesEstadoCremacion",
                column: "ComentarioOperativoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransicionesEstadoCremacion_CremacionId_Secuencia",
                table: "TransicionesEstadoCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransicionesEstadoCremacion");
        }
    }
}
