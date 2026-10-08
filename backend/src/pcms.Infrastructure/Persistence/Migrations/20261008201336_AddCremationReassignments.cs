using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationReassignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReasignacionesCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnteriorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    NombreAnteriorSnapshot = table.Column<string>(type: "text", nullable: true),
                    NuevoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreNuevoSnapshot = table.Column<string>(type: "text", nullable: false),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    RolActor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasignacionesCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReasignacionesCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReasignacionesCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReasignacionesCremacion_Usuarios_AnteriorUsuarioId",
                        column: x => x.AnteriorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReasignacionesCremacion_Usuarios_NuevoUsuarioId",
                        column: x => x.NuevoUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReasignacionesCremacion_ActorUsuarioId",
                table: "ReasignacionesCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReasignacionesCremacion_AnteriorUsuarioId",
                table: "ReasignacionesCremacion",
                column: "AnteriorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReasignacionesCremacion_CremacionId_RequestId",
                table: "ReasignacionesCremacion",
                columns: new[] { "CremacionId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReasignacionesCremacion_CremacionId_Secuencia",
                table: "ReasignacionesCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReasignacionesCremacion_NuevoUsuarioId",
                table: "ReasignacionesCremacion",
                column: "NuevoUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReasignacionesCremacion");
        }
    }
}
