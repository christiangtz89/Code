using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveReceptionIdentityAndPetDeathDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreClienteSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreMascotaSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: true);

            // Earlier identity values were not retained. This captures the currently
            // related master-record names once; edits made before this migration
            // cannot be reconstructed.
            migrationBuilder.Sql(
                """
                UPDATE "Recepciones" AS r
                SET "NombreMascotaSnapshot" = p."Nombre",
                    "NombreClienteSnapshot" = concat_ws(
                        ' ',
                        NULLIF(BTRIM(c."Nombre"), ''),
                        NULLIF(BTRIM(c."ApellidoPaterno"), ''),
                        NULLIF(BTRIM(c."ApellidoMaterno"), ''))
                FROM "Mascotas" AS p
                INNER JOIN "Clientes" AS c ON c."Id" = p."CustomerId"
                WHERE p."Id" = r."MascotaId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NombreClienteSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreMascotaSnapshot",
                table: "Recepciones",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Existing clients encoded calendar selections as UTC instants whose UTC
            // date was the selected value. Preserve that component during conversion.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Mascotas"
                ALTER COLUMN "FechaFallecimiento" TYPE date
                USING ("FechaFallecimiento" AT TIME ZONE 'UTC')::date;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreClienteSnapshot",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "NombreMascotaSnapshot",
                table: "Recepciones");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Mascotas"
                ALTER COLUMN "FechaFallecimiento" TYPE timestamp with time zone
                USING ("FechaFallecimiento"::timestamp AT TIME ZONE 'UTC');
                """);
        }
    }
}
