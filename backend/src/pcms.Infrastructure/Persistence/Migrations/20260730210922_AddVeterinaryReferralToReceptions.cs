using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVeterinaryReferralToReceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotasReferencia",
                table: "Recepciones",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VeterinariaId",
                table: "Recepciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VeterinarioReferenteId",
                table: "Recepciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_VeterinariaId",
                table: "Recepciones",
                column: "VeterinariaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recepciones_VeterinarioReferenteId",
                table: "Recepciones",
                column: "VeterinarioReferenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recepciones_Veterinarias_VeterinariaId",
                table: "Recepciones",
                column: "VeterinariaId",
                principalTable: "Veterinarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recepciones_Veterinarios_VeterinarioReferenteId",
                table: "Recepciones",
                column: "VeterinarioReferenteId",
                principalTable: "Veterinarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recepciones_Veterinarias_VeterinariaId",
                table: "Recepciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Recepciones_Veterinarios_VeterinarioReferenteId",
                table: "Recepciones");

            migrationBuilder.DropIndex(
                name: "IX_Recepciones_VeterinariaId",
                table: "Recepciones");

            migrationBuilder.DropIndex(
                name: "IX_Recepciones_VeterinarioReferenteId",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "NotasReferencia",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "VeterinariaId",
                table: "Recepciones");

            migrationBuilder.DropColumn(
                name: "VeterinarioReferenteId",
                table: "Recepciones");
        }
    }
}
