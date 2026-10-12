using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationArchiveEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosArchivoCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivoAnterior = table.Column<bool>(type: "boolean", nullable: false),
                    ActivoNuevo = table.Column<bool>(type: "boolean", nullable: false),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    RolActor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosArchivoCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosArchivoCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosArchivoCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosArchivoCremacion_ActorUsuarioId",
                table: "EventosArchivoCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosArchivoCremacion_CremacionId_Secuencia",
                table: "EventosArchivoCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosArchivoCremacion_RequestId",
                table: "EventosArchivoCremacion",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosArchivoCremacion");
        }
    }
}
