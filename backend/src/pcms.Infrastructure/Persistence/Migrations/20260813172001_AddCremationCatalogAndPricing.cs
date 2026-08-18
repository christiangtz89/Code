using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCremationCatalogAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionAccesorio",
                table: "Cremaciones",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaqueteCremacionId",
                table: "Cremaciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UrnaId",
                table: "Cremaciones",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionPreciosCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntervaloPesoKg = table.Column<int>(type: "integer", nullable: false),
                    PermitirIndividualSinCenizas = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionPreciosCremacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaquetesCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DescripcionCorta = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TipoPaquete = table.Column<int>(type: "integer", nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: true),
                    IncluyeUrna = table.Column<bool>(type: "boolean", nullable: false),
                    IncluyeHuella = table.Column<bool>(type: "boolean", nullable: false),
                    DescripcionAccesorio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IncluyeCertificado = table.Column<bool>(type: "boolean", nullable: false),
                    UrlImagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EsPublico = table.Column<bool>(type: "boolean", nullable: false),
                    OrdenVisualizacion = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaquetesCremacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Urnas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UrlImagen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Publico = table.Column<bool>(type: "boolean", nullable: false),
                    OrdenVisualizacion = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urnas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreciosCremacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaqueteCremacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoCremacion = table.Column<int>(type: "integer", nullable: false),
                    PesoMinimoKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PesoMaximoKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciosCremacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreciosCremacion_PaquetesCremacion_PaqueteCremacionId",
                        column: x => x.PaqueteCremacionId,
                        principalTable: "PaquetesCremacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_PaqueteCremacionId",
                table: "Cremaciones",
                column: "PaqueteCremacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cremaciones_UrnaId",
                table: "Cremaciones",
                column: "UrnaId");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacion_Activo",
                table: "PaquetesCremacion",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacion_EsPublico",
                table: "PaquetesCremacion",
                column: "EsPublico");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacion_Nombre",
                table: "PaquetesCremacion",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCremacion_OrdenVisualizacion",
                table: "PaquetesCremacion",
                column: "OrdenVisualizacion");

            migrationBuilder.CreateIndex(
                name: "IX_PreciosCremacion_Activo",
                table: "PreciosCremacion",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PreciosCremacion_PaqueteCremacionId_TipoCremacion_PesoMinim~",
                table: "PreciosCremacion",
                columns: new[] { "PaqueteCremacionId", "TipoCremacion", "PesoMinimoKg", "PesoMaximoKg" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Urnas_Activo",
                table: "Urnas",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Urnas_Nombre",
                table: "Urnas",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Urnas_OrdenVisualizacion",
                table: "Urnas",
                column: "OrdenVisualizacion");

            migrationBuilder.CreateIndex(
                name: "IX_Urnas_Publico",
                table: "Urnas",
                column: "Publico");

            migrationBuilder.AddForeignKey(
                name: "FK_Cremaciones_PaquetesCremacion_PaqueteCremacionId",
                table: "Cremaciones",
                column: "PaqueteCremacionId",
                principalTable: "PaquetesCremacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cremaciones_Urnas_UrnaId",
                table: "Cremaciones",
                column: "UrnaId",
                principalTable: "Urnas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cremaciones_PaquetesCremacion_PaqueteCremacionId",
                table: "Cremaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Cremaciones_Urnas_UrnaId",
                table: "Cremaciones");

            migrationBuilder.DropTable(
                name: "ConfiguracionPreciosCremacion");

            migrationBuilder.DropTable(
                name: "PreciosCremacion");

            migrationBuilder.DropTable(
                name: "Urnas");

            migrationBuilder.DropTable(
                name: "PaquetesCremacion");

            migrationBuilder.DropIndex(
                name: "IX_Cremaciones_PaqueteCremacionId",
                table: "Cremaciones");

            migrationBuilder.DropIndex(
                name: "IX_Cremaciones_UrnaId",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "DescripcionAccesorio",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "PaqueteCremacionId",
                table: "Cremaciones");

            migrationBuilder.DropColumn(
                name: "UrnaId",
                table: "Cremaciones");
        }
    }
}
