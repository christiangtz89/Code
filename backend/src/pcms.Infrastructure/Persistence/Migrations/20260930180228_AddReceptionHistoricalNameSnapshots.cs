using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionHistoricalNameSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioRecibioSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinariaSnapshot",
                table: "Recepciones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: true);

            // Preserve only names supported by immutable history or source
            // snapshots. Never infer a historical name from current master data.
            migrationBuilder.Sql(
                """
                WITH latest_clinic_change AS (
                    SELECT DISTINCT ON (history_event."RecepcionId")
                        history_event."RecepcionId",
                        history_change."ValorNuevo",
                        history_change."ValorNuevoMostrado"
                    FROM "CambiosHistorialRecepcion" AS history_change
                    INNER JOIN "EventosHistorialRecepcion" AS history_event
                        ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                    WHERE history_change."Campo" = 2
                    ORDER BY
                        history_event."RecepcionId",
                        history_event."NumeroSecuencia" DESC
                )
                UPDATE "Recepciones" AS reception
                SET "NombreVeterinariaSnapshot" = latest."ValorNuevoMostrado"
                FROM latest_clinic_change AS latest
                WHERE latest."RecepcionId" = reception."Id"
                  AND reception."VeterinariaId" IS NOT NULL
                  AND BTRIM(latest."ValorNuevo") ~ '^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$'
                  AND LOWER(BTRIM(latest."ValorNuevo")) = LOWER(reception."VeterinariaId"::text)
                  AND latest."ValorNuevoMostrado" ~ '[^[:space:]]'
                  AND CHAR_LENGTH(latest."ValorNuevoMostrado") <= 200;

                WITH latest_veterinarian_change AS (
                    SELECT DISTINCT ON (history_event."RecepcionId")
                        history_event."RecepcionId",
                        history_change."ValorNuevo",
                        history_change."ValorNuevoMostrado"
                    FROM "CambiosHistorialRecepcion" AS history_change
                    INNER JOIN "EventosHistorialRecepcion" AS history_event
                        ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                    WHERE history_change."Campo" = 3
                    ORDER BY
                        history_event."RecepcionId",
                        history_event."NumeroSecuencia" DESC
                )
                UPDATE "Recepciones" AS reception
                SET "NombreVeterinarioReferenteSnapshot" = latest."ValorNuevoMostrado"
                FROM latest_veterinarian_change AS latest
                WHERE latest."RecepcionId" = reception."Id"
                  AND reception."VeterinarioReferenteId" IS NOT NULL
                  AND BTRIM(latest."ValorNuevo") ~ '^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$'
                  AND LOWER(BTRIM(latest."ValorNuevo")) = LOWER(reception."VeterinarioReferenteId"::text)
                  AND latest."ValorNuevoMostrado" ~ '[^[:space:]]';

                UPDATE "Recepciones" AS reception
                SET "NombreVeterinariaSnapshot" = request."NombreVeterinariaSnapshot"
                FROM "SolicitudesVeterinarias" AS request
                WHERE request."RecepcionId" = reception."Id"
                  AND reception."VeterinariaId" IS NOT NULL
                  AND request."VeterinariaId" = reception."VeterinariaId"
                  AND request."NombreVeterinariaSnapshot" ~ '[^[:space:]]'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CambiosHistorialRecepcion" AS history_change
                      INNER JOIN "EventosHistorialRecepcion" AS history_event
                          ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                      WHERE history_event."RecepcionId" = reception."Id"
                        AND history_change."Campo" = 2);

                UPDATE "Recepciones" AS reception
                SET "NombreVeterinarioReferenteSnapshot" = request."NombreVeterinarioReferenteSnapshot"
                FROM "SolicitudesVeterinarias" AS request
                WHERE request."RecepcionId" = reception."Id"
                  AND reception."VeterinarioReferenteId" IS NOT NULL
                  AND request."VeterinarioReferenteId" = reception."VeterinarioReferenteId"
                  AND request."NombreVeterinarioReferenteSnapshot" ~ '[^[:space:]]'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CambiosHistorialRecepcion" AS history_change
                      INNER JOIN "EventosHistorialRecepcion" AS history_event
                          ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                      WHERE history_event."RecepcionId" = reception."Id"
                        AND history_change."Campo" = 3);

                UPDATE "Recepciones" AS reception
                SET "NombreVeterinariaSnapshot" = collection."NombreVeterinariaSnapshot"
                FROM "Recolecciones" AS collection
                WHERE collection."Id" = reception."RecoleccionId"
                  AND reception."VeterinariaId" IS NOT NULL
                  AND collection."VeterinariaId" = reception."VeterinariaId"
                  AND collection."NombreVeterinariaSnapshot" ~ '[^[:space:]]'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "SolicitudesVeterinarias" AS request
                      WHERE request."RecepcionId" = reception."Id")
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CambiosHistorialRecepcion" AS history_change
                      INNER JOIN "EventosHistorialRecepcion" AS history_event
                          ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                      WHERE history_event."RecepcionId" = reception."Id"
                        AND history_change."Campo" = 2);

                UPDATE "Recepciones" AS reception
                SET "NombreVeterinarioReferenteSnapshot" = collection."NombreVeterinarioReferenteSnapshot"
                FROM "Recolecciones" AS collection
                WHERE collection."Id" = reception."RecoleccionId"
                  AND reception."VeterinarioReferenteId" IS NOT NULL
                  AND collection."VeterinarioReferenteId" = reception."VeterinarioReferenteId"
                  AND collection."NombreVeterinarioReferenteSnapshot" ~ '[^[:space:]]'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "SolicitudesVeterinarias" AS request
                      WHERE request."RecepcionId" = reception."Id")
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CambiosHistorialRecepcion" AS history_change
                      INNER JOIN "EventosHistorialRecepcion" AS history_event
                          ON history_event."Id" = history_change."EventoHistorialRecepcionId"
                      WHERE history_event."RecepcionId" = reception."Id"
                        AND history_change."Campo" = 3);

                UPDATE "Recepciones" AS reception
                SET "NombreUsuarioRecibioSnapshot" = collection."NombreUsuarioRecibioSnapshot"
                FROM "Recolecciones" AS collection
                WHERE collection."Id" = reception."RecoleccionId"
                  AND collection."RecibidoPorUsuarioId" = reception."RecibidoPorUsuarioId"
                  AND collection."NombreUsuarioRecibioSnapshot" ~ '[^[:space:]]';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreUsuarioRecibioSnapshot",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "NombreVeterinariaSnapshot",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "Recepciones");
        }
    }
}
