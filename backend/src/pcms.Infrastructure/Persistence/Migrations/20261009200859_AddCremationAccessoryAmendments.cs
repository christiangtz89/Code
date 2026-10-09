using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationAccessoryAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CambiosAccesorioCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Secuencia = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DescripcionAnterior = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescripcionNueva = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreActorSnapshot = table.Column<string>(type: "text", nullable: false),
                    RolActor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosAccesorioCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CambiosAccesorioCremacion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CambiosAccesorioCremacion_Usuarios_ActorUsuarioId",
                        column: x => x.ActorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosAccesorioCremacion_ActorUsuarioId",
                table: "CambiosAccesorioCremacion",
                column: "ActorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CambiosAccesorioCremacion_CremacionId_RequestId",
                table: "CambiosAccesorioCremacion",
                columns: new[] { "CremacionId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CambiosAccesorioCremacion_CremacionId_Secuencia",
                table: "CambiosAccesorioCremacion",
                columns: new[] { "CremacionId", "Secuencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosAccesorioCremacion");
        }
    }
}
