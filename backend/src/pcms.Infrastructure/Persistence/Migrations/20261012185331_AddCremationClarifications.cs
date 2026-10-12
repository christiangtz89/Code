using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationClarifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AclaracionesCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    RolActor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AclaracionesCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AclaracionesCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AclaracionesCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AclaracionesCremacion_ActorUsuarioId",
                table: "AclaracionesCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AclaracionesCremacion_CremacionId_Secuencia",
                table: "AclaracionesCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AclaracionesCremacion_RequestId",
                table: "AclaracionesCremacion",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AclaracionesCremacion");
        }
    }
}
