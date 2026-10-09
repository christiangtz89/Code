using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationInstructionsAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CambiosInstruccionesCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstruccionesAnteriores = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InstruccionesNuevas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    RolActor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosInstruccionesCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CambiosInstruccionesCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CambiosInstruccionesCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosInstruccionesCremacion_ActorUsuarioId",
                table: "CambiosInstruccionesCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CambiosInstruccionesCremacion_CremacionId_RequestId",
                table: "CambiosInstruccionesCremacion",
                columns: new[] { "CremacionId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CambiosInstruccionesCremacion_CremacionId_Secuencia",
                table: "CambiosInstruccionesCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosInstruccionesCremacion");
        }
    }
}
