using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionHistoryAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosHistorialRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumeroSecuencia = table.Column<long>(type: "bigint", nullable: false),
                    TipoEvento = table.Column<int>(type: "integer", nullable: false),
                    EtapaRecepcion = table.Column<int>(type: "integer", nullable: false),
                    EstadoCremacionSnapshot = table.Column<int>(type: "integer", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TextoAclaracion = table.Column<string>(type: "text", nullable: true),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarioCreoSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RolUsuarioCreoSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosHistorialRecepcion", x => x.Id);
                    table.CheckConstraint("CK_EventosHistorialRecepcion_ContenidoTipoEvento", "(\"TipoEvento\" = 1 AND \"EtapaRecepcion\" = 1 AND \"SolicitudId\" IS NULL AND \"Motivo\" IS NULL AND \"TextoAclaracion\" IS NULL) OR (\"TipoEvento\" = 2 AND \"EtapaRecepcion\" IN (2, 3, 4) AND \"SolicitudId\" IS NOT NULL AND NULLIF(BTRIM(\"Motivo\"), '') IS NOT NULL AND \"TextoAclaracion\" IS NULL) OR (\"TipoEvento\" = 3 AND \"EtapaRecepcion\" IN (2, 3, 4) AND \"SolicitudId\" IS NOT NULL AND \"Motivo\" IS NULL AND NULLIF(BTRIM(\"TextoAclaracion\"), '') IS NOT NULL)");
                    table.CheckConstraint("CK_EventosHistorialRecepcion_EstadoCremacion", "\"EstadoCremacionSnapshot\" IS NULL OR \"EstadoCremacionSnapshot\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9)");
                    table.CheckConstraint("CK_EventosHistorialRecepcion_EtapaCremacion", "(\"EtapaRecepcion\" = 1 AND \"CremacionId\" IS NULL AND \"EstadoCremacionSnapshot\" IS NULL) OR (\"EtapaRecepcion\" IN (2, 3, 4) AND \"CremacionId\" IS NOT NULL AND \"EstadoCremacionSnapshot\" IS NOT NULL)");
                    table.CheckConstraint("CK_EventosHistorialRecepcion_EtapaRecepcion", "\"EtapaRecepcion\" IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_EventosHistorialRecepcion_NumeroSecuencia", "\"NumeroSecuencia\" > 0");
                    table.CheckConstraint("CK_EventosHistorialRecepcion_TipoEvento", "\"TipoEvento\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_EventosHistorialRecepcion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosHistorialRecepcion_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosHistorialRecepcion_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CambiosHistorialRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventoHistorialRecepcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Campo = table.Column<int>(type: "integer", nullable: false),
                    ValorOriginal = table.Column<string>(type: "text", nullable: true),
                    ValorNuevo = table.Column<string>(type: "text", nullable: true),
                    ValorOriginalMostrado = table.Column<string>(type: "text", nullable: true),
                    ValorNuevoMostrado = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosHistorialRecepcion", x => x.Id);
                    table.CheckConstraint("CK_CambiosHistorialRecepcion_CambioReal", "\"ValorOriginal\" IS DISTINCT FROM \"ValorNuevo\" OR \"ValorOriginalMostrado\" IS DISTINCT FROM \"ValorNuevoMostrado\"");
                    table.CheckConstraint("CK_CambiosHistorialRecepcion_Campo", "\"Campo\" IN (1, 2, 3, 4, 5, 6)");
                    table.ForeignKey(
                        name: "FK_CambiosHistorialRecepcion_EventosHistorialRecepcion_EventoH~",
                        column: x => x.EventoHistorialRecepcionId,
                        principalTable: "EventosHistorialRecepcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[] { new Guid("00000035-0000-0000-0000-000000000001"), "Receptions.Amend", "Enmendar recepciones" });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("00000035-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.CreateIndex(
                name: "IX_CambiosHistorialRecepcion_EventoHistorialRecepcionId_Campo",
                table: "CambiosHistorialRecepcion",
                columns: new[] { "EventoHistorialRecepcionId", "Campo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosHistorialRecepcion_CreadoPorUsuarioId",
                table: "EventosHistorialRecepcion",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosHistorialRecepcion_CremacionId",
                table: "EventosHistorialRecepcion",
                column: "CremacionId",
                filter: "\"CremacionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosHistorialRecepcion_RecepcionId_FechaCreacion_Id",
                table: "EventosHistorialRecepcion",
                columns: new[] { "RecepcionId", "FechaCreacion", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosHistorialRecepcion_RecepcionId_NumeroSecuencia",
                table: "EventosHistorialRecepcion",
                columns: new[] { "RecepcionId", "NumeroSecuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosHistorialRecepcion_RecepcionId_SolicitudId",
                table: "EventosHistorialRecepcion",
                columns: new[] { "RecepcionId", "SolicitudId" },
                unique: true,
                filter: "\"SolicitudId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosHistorialRecepcion");

            migrationBuilder.DropTable(
                name: "EventosHistorialRecepcion");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000035-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000001"));
        }
    }
}
