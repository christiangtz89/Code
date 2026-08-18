using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationPackageUrnAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaquetesCremacionUrnas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteCremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UrnaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaquetesCremacionUrnas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaquetesCremacionUrnas_PaquetesCremacion_PaqueteCremacionId",
                        column: x => x.PaqueteCremacionId,
                        principalTable: "PaquetesCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaquetesCremacionUrnas_Urnas_UrnaId",
                        column: x => x.UrnaId,
                        principalTable: "Urnas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacionUrnas_Activo",
                table: "PaquetesCremacionUrnas",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacionUrnas_PaqueteCremacionId_UrnaId",
                table: "PaquetesCremacionUrnas",
                columns: new[] { "PaqueteCremacionId", "UrnaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacionUrnas_UrnaId",
                table: "PaquetesCremacionUrnas",
                column: "UrnaId");

            migrationBuilder.Sql(
                """
                INSERT INTO "PaquetesCremacionUrnas"
                    ("Id", "PaqueteCremacionId", "UrnaId", "Activo", "FechaCreacion", "FechaActualizacion")
                SELECT
                    gen_random_uuid(),
                    paquete."Id",
                    urna."Id",
                    TRUE,
                    CURRENT_TIMESTAMP,
                    NULL
                FROM "PaquetesCremacion" AS paquete
                CROSS JOIN "Urnas" AS urna
                WHERE paquete."IncluyeUrna" = TRUE
                  AND urna."Activo" = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaquetesCremacionUrnas");
        }
    }
}
