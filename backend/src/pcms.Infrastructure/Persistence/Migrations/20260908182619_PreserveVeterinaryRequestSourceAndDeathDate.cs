using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveVeterinaryRequestSourceAndDeathDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinariaSnapshot",
                table: "SolicitudesVeterinarias",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "SolicitudesVeterinarias",
                type: "text",
                nullable: true);

            // Earlier rows never stored immutable source names, so their
            // snapshots can only be initialized from current master data.
            migrationBuilder.Sql(
                """
                UPDATE "SolicitudesVeterinarias" AS request
                SET "NombreVeterinariaSnapshot" = clinic."Nombre"
                FROM "Veterinarias" AS clinic
                WHERE request."VeterinariaId" = clinic."Id";

                UPDATE "SolicitudesVeterinarias" AS request
                SET "NombreVeterinarioReferenteSnapshot" = CONCAT_WS(
                    ' ',
                    BTRIM(veterinarian."Nombre"),
                    BTRIM(veterinarian."ApellidoPaterno"),
                    NULLIF(BTRIM(veterinarian."ApellidoMaterno"), ''))
                FROM "Veterinarios" AS veterinarian
                WHERE request."VeterinarioReferenteId" = veterinarian."Id";
                """);

            // Existing clients stored the selected YYYY-MM-DD as midnight
            // UTC. Extract the UTC calendar component to avoid a local-time
            // shift to the preceding Mexico date.
            migrationBuilder.Sql(
                """
                ALTER TABLE "SolicitudesVeterinarias"
                ALTER COLUMN "FechaFallecimiento" TYPE date
                USING ("FechaFallecimiento" AT TIME ZONE 'UTC')::date;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreVeterinariaSnapshot",
                table: "SolicitudesVeterinarias");

            migrationBuilder.DropColumn(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "SolicitudesVeterinarias");

            migrationBuilder.Sql(
                """
                ALTER TABLE "SolicitudesVeterinarias"
                ALTER COLUMN "FechaFallecimiento" TYPE timestamp with time zone
                USING "FechaFallecimiento"::timestamp AT TIME ZONE 'UTC';
                """);
        }
    }
}
