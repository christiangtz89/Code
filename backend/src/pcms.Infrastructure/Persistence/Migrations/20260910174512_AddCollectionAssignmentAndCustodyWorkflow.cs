using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionAssignmentAndCustodyWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RecolectadoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRecoleccion",
                table: "Recolecciones",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "AceptadoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AsignadoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConductorAsignadoId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptacion",
                table: "Recolecciones",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAsignacion",
                table: "Recolecciones",
                type: "timestamp with time zone",
                nullable: true);

            // Legacy rows keep their existing Estado, RecolectadoPorUsuarioId,
            // and FechaRecoleccion as the only historically known custody facts.
            // Assignment and acceptance remain null because those events were
            // not recorded before this migration and must not be fabricated.

            migrationBuilder.CreateTable(
                name: "HistorialAsignacionesRecoleccion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecoleccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConductorAsignadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsignadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AceptadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaAceptacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaFinalizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialAsignacionesRecoleccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialAsignacionesRecoleccion_Recolecciones_RecoleccionId",
                        column: x => x.RecoleccionId,
                        principalTable: "Recolecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialAsignacionesRecoleccion_Usuarios_AceptadoPorUsuari~",
                        column: x => x.AceptadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialAsignacionesRecoleccion_Usuarios_AsignadoPorUsuari~",
                        column: x => x.AsignadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialAsignacionesRecoleccion_Usuarios_ConductorAsignado~",
                        column: x => x.ConductorAsignadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialAsignacionesRecoleccion_Usuarios_FinalizadoPorUsua~",
                        column: x => x.FinalizadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_AceptadoPorUsuarioId",
                table: "Recolecciones",
                column: "AceptadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_AsignadoPorUsuarioId",
                table: "Recolecciones",
                column: "AsignadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_ConductorAsignadoId",
                table: "Recolecciones",
                column: "ConductorAsignadoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAsignacionesRecoleccion_AceptadoPorUsuarioId",
                table: "HistorialAsignacionesRecoleccion",
                column: "AceptadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAsignacionesRecoleccion_AsignadoPorUsuarioId",
                table: "HistorialAsignacionesRecoleccion",
                column: "AsignadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAsignacionesRecoleccion_ConductorAsignadoId",
                table: "HistorialAsignacionesRecoleccion",
                column: "ConductorAsignadoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAsignacionesRecoleccion_FinalizadoPorUsuarioId",
                table: "HistorialAsignacionesRecoleccion",
                column: "FinalizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAsignacionesRecoleccion_RecoleccionId",
                table: "HistorialAsignacionesRecoleccion",
                column: "RecoleccionId",
                unique: true,
                filter: "\"FechaFinalizacion\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_AceptadoPorUsuarioId",
                table: "Recolecciones",
                column: "AceptadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_AsignadoPorUsuarioId",
                table: "Recolecciones",
                column: "AsignadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_ConductorAsignadoId",
                table: "Recolecciones",
                column: "ConductorAsignadoId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Recolecciones"
                        WHERE "RecolectadoPorUsuarioId" IS NULL
                           OR "FechaRecoleccion" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot remove the collection custody workflow while pre-custody collections exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_AceptadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_AsignadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_ConductorAsignadoId",
                table: "Recolecciones");

            migrationBuilder.DropTable(
                name: "HistorialAsignacionesRecoleccion");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_AceptadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_AsignadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_ConductorAsignadoId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "AceptadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "AsignadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "ConductorAsignadoId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "FechaAceptacion",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "FechaAsignacion",
                table: "Recolecciones");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecolectadoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRecoleccion",
                table: "Recolecciones",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
