using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedWeightRepricingAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacionCambioRango",
                table: "CuentasPago",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResolucionRevisionFinanciera",
                table: "CuentasPago",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoResolucionRevisionFinanciera",
                table: "CuentasPago",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioConfirmoCambioRangoSnapshot",
                table: "CuentasPago",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreUsuarioResolvioRevisionFinancieraSnapshot",
                table: "CuentasPago",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMaximoProvisionalSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoMinimoProvisionalSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoProvisionalSnapshotKg",
                table: "CuentasPago",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrecioCremacionProvisionalId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereRevisionFinanciera",
                table: "CuentasPago",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalServicioProvisional",
                table: "CuentasPago",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago",
                column: "CambioRangoConfirmadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_PrecioCremacionProvisionalId",
                table: "CuentasPago",
                column: "PrecioCremacionProvisionalId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago",
                column: "RevisionFinancieraResueltaPorUsuarioId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuentasPago_ResolucionRevisionFinanciera",
                table: "CuentasPago",
                sql: "(\"RevisionFinancieraResueltaPorUsuarioId\" IS NULL AND \"NombreUsuarioResolvioRevisionFinancieraSnapshot\" IS NULL AND \"FechaResolucionRevisionFinanciera\" IS NULL AND \"MotivoResolucionRevisionFinanciera\" IS NULL) OR (\"RequiereRevisionFinanciera\" = TRUE AND \"RevisionFinancieraResueltaPorUsuarioId\" IS NOT NULL AND NULLIF(BTRIM(\"NombreUsuarioResolvioRevisionFinancieraSnapshot\"), '') IS NOT NULL AND \"FechaResolucionRevisionFinanciera\" IS NOT NULL AND NULLIF(BTRIM(\"MotivoResolucionRevisionFinanciera\"), '') IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_PreciosCremacion_PrecioCremacionProvisionalId",
                table: "CuentasPago",
                column: "PrecioCremacionProvisionalId",
                principalTable: "PreciosCremacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_Usuarios_CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago",
                column: "CambioRangoConfirmadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_Usuarios_RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago",
                column: "RevisionFinancieraResueltaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_PreciosCremacion_PrecioCremacionProvisionalId",
                table: "CuentasPago");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_Usuarios_CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_Usuarios_RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_PrecioCremacionProvisionalId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuentasPago_ResolucionRevisionFinanciera",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "CambioRangoConfirmadoPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacionCambioRango",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "FechaResolucionRevisionFinanciera",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "MotivoResolucionRevisionFinanciera",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioConfirmoCambioRangoSnapshot",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "NombreUsuarioResolvioRevisionFinancieraSnapshot",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PesoMaximoProvisionalSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PesoMinimoProvisionalSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PesoProvisionalSnapshotKg",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PrecioCremacionProvisionalId",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "RequiereRevisionFinanciera",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "RevisionFinancieraResueltaPorUsuarioId",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "TotalServicioProvisional",
                table: "CuentasPago");
        }
    }
}
