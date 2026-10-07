using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCremationStartPaymentRevalidationReplayEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NombreUsuarioConfirmoSnapshot",
                table: "RevalidacionesPagoInicioCremacion",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<bool>(
                name: "CambioPrecioConfirmado",
                table: "RevalidacionesPagoInicioCremacion",
                type: "boolean",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CambioPrecioConfirmado",
                table: "RevalidacionesPagoInicioCremacion");

            migrationBuilder.AlterColumn<string>(
                name: "NombreUsuarioConfirmoSnapshot",
                table: "RevalidacionesPagoInicioCremacion",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
