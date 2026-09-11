using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCollectionPaymentBeforeReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagoRequeridoRecoleccion",
                table: "PreciosCremacion",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CremacionId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagoRequeridoRecoleccion",
                table: "CuentasPago",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombrePaqueteSnapshot",
                table: "CuentasPago",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaqueteCremacionId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrecioCremacionId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecoleccionId",
                table: "CuentasPago",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_PaqueteCremacionId",
                table: "CuentasPago",
                column: "PaqueteCremacionId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_PrecioCremacionId",
                table: "CuentasPago",
                column: "PrecioCremacionId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPago_RecoleccionId",
                table: "CuentasPago",
                column: "RecoleccionId",
                unique: true,
                filter: "\"RecoleccionId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CuentasPago_Contexto",
                table: "CuentasPago",
                sql: "\"CremacionId\" IS NOT NULL OR \"RecoleccionId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_PaquetesCremacion_PaqueteCremacionId",
                table: "CuentasPago",
                column: "PaqueteCremacionId",
                principalTable: "PaquetesCremacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_PreciosCremacion_PrecioCremacionId",
                table: "CuentasPago",
                column: "PrecioCremacionId",
                principalTable: "PreciosCremacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasPago_Recolecciones_RecoleccionId",
                table: "CuentasPago",
                column: "RecoleccionId",
                principalTable: "Recolecciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_PaquetesCremacion_PaqueteCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_PreciosCremacion_PrecioCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentasPago_Recolecciones_RecoleccionId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_PaqueteCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_PrecioCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropIndex(
                name: "IX_CuentasPago_RecoleccionId",
                table: "CuentasPago");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CuentasPago_Contexto",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "MontoPagoRequeridoRecoleccion",
                table: "PreciosCremacion");

            migrationBuilder.DropColumn(
                name: "MontoPagoRequeridoRecoleccion",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "NombrePaqueteSnapshot",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PaqueteCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "PrecioCremacionId",
                table: "CuentasPago");

            migrationBuilder.DropColumn(
                name: "RecoleccionId",
                table: "CuentasPago");

            migrationBuilder.AlterColumn<Guid>(
                name: "CremacionId",
                table: "CuentasPago",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
