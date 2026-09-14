using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveCollectionHistoricalSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CanceladoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreadoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecieMascotaSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreClienteSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreConductorAsignadoSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreMascotaSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioAceptoSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioAsignoSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioCanceloSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioCreoSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioRecibioSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioRecolectoSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinariaSnapshot",
                table: "Recolecciones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecibidoPorUsuarioId",
                table: "Recolecciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoClienteSnapshot",
                table: "Recolecciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreConductorAsignadoSnapshot",
                table: "HistorialAsignacionesRecoleccion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioAceptoSnapshot",
                table: "HistorialAsignacionesRecoleccion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioAsignoSnapshot",
                table: "HistorialAsignacionesRecoleccion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioFinalizoSnapshot",
                table: "HistorialAsignacionesRecoleccion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioSubioSnapshot",
                table: "FotosRecoleccion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoBaseSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMaximoSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMinimoSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoCremacionSnapshot",
                table: "CuentasPago",
                type: "integer",
                nullable: true);

            // Historical exactness before this migration is limited to the
            // current related data because these immutable values were not
            // previously persisted.
            migrationBuilder.Sql("""
                UPDATE "Recolecciones" AS collection
                SET "NombreClienteSnapshot" = concat_ws(' ', customer."Nombre", customer."ApellidoPaterno", customer."ApellidoMaterno"),
                    "TelefonoClienteSnapshot" = customer."Telefono",
                    "NombreMascotaSnapshot" = pet."Nombre",
                    "EspecieMascotaSnapshot" = pet."Especie",
                    "NombreVeterinariaSnapshot" = (SELECT clinic."Nombre" FROM "Veterinarias" AS clinic WHERE clinic."Id" = collection."VeterinariaId"),
                    "NombreVeterinarioReferenteSnapshot" = (SELECT concat_ws(' ', veterinarian."Nombre", veterinarian."ApellidoPaterno", veterinarian."ApellidoMaterno") FROM "Veterinarios" AS veterinarian WHERE veterinarian."Id" = collection."VeterinarioReferenteId"),
                    "NombreUsuarioRecolectoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = collection."RecolectadoPorUsuarioId"),
                    "NombreConductorAsignadoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = collection."ConductorAsignadoId"),
                    "NombreUsuarioAsignoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = collection."AsignadoPorUsuarioId"),
                    "NombreUsuarioAceptoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = collection."AceptadoPorUsuarioId"),
                    "RecibidoPorUsuarioId" = (SELECT reception."RecibidoPorUsuarioId" FROM "Recepciones" AS reception WHERE reception."RecoleccionId" = collection."Id"),
                    "NombreUsuarioRecibioSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Recepciones" AS reception JOIN "Usuarios" AS actor ON actor."Id" = reception."RecibidoPorUsuarioId" WHERE reception."RecoleccionId" = collection."Id")
                FROM "Mascotas" AS pet
                JOIN "Clientes" AS customer ON customer."Id" = pet."CustomerId"
                WHERE collection."MascotaId" = pet."Id";

                UPDATE "HistorialAsignacionesRecoleccion" AS history
                SET "NombreConductorAsignadoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = history."ConductorAsignadoId"),
                    "NombreUsuarioAsignoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = history."AsignadoPorUsuarioId"),
                    "NombreUsuarioAceptoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = history."AceptadoPorUsuarioId"),
                    "NombreUsuarioFinalizoSnapshot" = (SELECT concat_ws(' ', actor."Nombre", actor."Apellido") FROM "Usuarios" AS actor WHERE actor."Id" = history."FinalizadoPorUsuarioId");

                UPDATE "FotosRecoleccion" AS photo
                SET "NombreUsuarioSubioSnapshot" = concat_ws(' ', uploader."Nombre", uploader."Apellido")
                FROM "Usuarios" AS uploader
                WHERE uploader."Id" = photo."SubidoPorUsuarioId";

                UPDATE "CuentasPago" AS account
                SET "TipoCremacionSnapshot" = price."TipoCremacion",
                    "PesoMinimoSnapshotKg" = price."PesoMinimoKg",
                    "PesoMaximoSnapshotKg" = price."PesoMaximoKg",
                    "PesoBaseSnapshotKg" = COALESCE(collection."PesoAproximadoKg", pet."PesoKg")
                FROM "PreciosCremacion" AS price, "Recolecciones" AS collection, "Mascotas" AS pet
                WHERE account."PrecioCremacionId" = price."Id"
                  AND collection."Id" = account."RecoleccionId"
                  AND pet."Id" = collection."MascotaId";
                """);

            migrationBuilder.AlterColumn<string>(name: "EspecieMascotaSnapshot", table: "Recolecciones", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NombreClienteSnapshot", table: "Recolecciones", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NombreMascotaSnapshot", table: "Recolecciones", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NombreConductorAsignadoSnapshot", table: "HistorialAsignacionesRecoleccion", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NombreUsuarioAsignoSnapshot", table: "HistorialAsignacionesRecoleccion", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "NombreUsuarioSubioSnapshot", table: "FotosRecoleccion", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_CanceladoPorUsuarioId",
                table: "Recolecciones",
                column: "CanceladoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_CreadoPorUsuarioId",
                table: "Recolecciones",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recolecciones_RecibidoPorUsuarioId",
                table: "Recolecciones",
                column: "RecibidoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_CanceladoPorUsuarioId",
                table: "Recolecciones",
                column: "CanceladoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_CreadoPorUsuarioId",
                table: "Recolecciones",
                column: "CreadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recolecciones_Usuarios_RecibidoPorUsuarioId",
                table: "Recolecciones",
                column: "RecibidoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_CanceladoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_CreadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Recolecciones_Usuarios_RecibidoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_CanceladoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_CreadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropIndex(
                name: "IX_Recolecciones_RecibidoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "CanceladoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "CreadoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "EspecieMascotaSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreClienteSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreConductorAsignadoSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreMascotaSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioAceptoSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioAsignoSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioCanceloSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioCreoSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioRecibioSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioRecolectoSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreVeterinariaSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreVeterinarioReferenteSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "RecibidoPorUsuarioId",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "TelefonoClienteSnapshot",
                table: "Recolecciones");

            migrationBuilder.DropColumn(
                name: "NombreConductorAsignadoSnapshot",
                table: "HistorialAsignacionesRecoleccion");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioAceptoSnapshot",
                table: "HistorialAsignacionesRecoleccion");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioAsignoSnapshot",
                table: "HistorialAsignacionesRecoleccion");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioFinalizoSnapshot",
                table: "HistorialAsignacionesRecoleccion");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioSubioSnapshot",
                table: "FotosRecoleccion");

            migrationBuilder.DropColumn(
                name: "PesoBaseSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PesoMaximoSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PesoMinimoSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "TipoCremacionSnapshot",
                table: "CuentasPago");
        }
    }
}
