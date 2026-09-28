using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionLifecycleAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosCicloVidaRecepcion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecepcionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroSecuencia = table.Column<long>(type: "bigint", nullable: false),
                    TipoAccion = table.Column<int>(type: "integer", nullable: false),
                    TipoResultado = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreUsuarioCreoSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RolUsuarioCreoSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecepcionActivaAnterior = table.Column<bool>(type: "boolean", nullable: false),
                    RecepcionActivaNueva = table.Column<bool>(type: "boolean", nullable: false),
                    EtapaOperativaSnapshot = table.Column<int>(type: "integer", nullable: false),
                    RecoleccionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecoleccionActivaSnapshot = table.Column<bool>(type: "boolean", nullable: true),
                    EstadoRecoleccionSnapshot = table.Column<int>(type: "integer", nullable: true),
                    FechaRecoleccionSnapshot = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRecepcionRecoleccionSnapshot = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TieneSolicitudVeterinariaConvertidaSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    SolicitudVeterinariaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CremacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CremacionActivaSnapshot = table.Column<bool>(type: "boolean", nullable: true),
                    EstadoCremacionSnapshot = table.Column<int>(type: "integer", nullable: true),
                    CuentaPagoId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalServicioSnapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    MontoPagadoSnapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    MontoPagoRequeridoRecoleccionSnapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    RequiereRevisionFinancieraSnapshot = table.Column<bool>(type: "boolean", nullable: true),
                    CantidadPagosSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CantidadEvidenciasRecepcionActivasSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CantidadEvidenciasRecoleccionSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CantidadHistorialAsignacionesRecoleccionSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CantidadHistorialRecepcionSnapshot = table.Column<int>(type: "integer", nullable: false),
                    UltimoNumeroSecuenciaHistorialRecepcionSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    DependenciasSnapshot = table.Column<int>(type: "integer", nullable: false),
                    MotivoBloqueoSnapshot = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosCicloVidaRecepcion", x => x.Id);
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_AccionResultado", "(\"TipoAccion\" = 1 AND \"TipoResultado\" = 1) OR (\"TipoAccion\" IN (2, 3) AND \"TipoResultado\" IN (2, 3))");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_ActorSnapshot", "NULLIF(BTRIM(\"NombreUsuarioCreoSnapshot\"), '') IS NOT NULL AND NULLIF(BTRIM(\"RolUsuarioCreoSnapshot\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_BloqueoDependencias", "\"MotivoBloqueoSnapshot\" <> 1 OR \"DependenciasSnapshot\" <> 0");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_ContextoHistorialRecepcion", "(\"CantidadHistorialRecepcionSnapshot\" = 0 AND \"UltimoNumeroSecuenciaHistorialRecepcionSnapshot\" IS NULL) OR (\"CantidadHistorialRecepcionSnapshot\" > 0 AND \"UltimoNumeroSecuenciaHistorialRecepcionSnapshot\" > 0)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_CremacionSnapshot", "(\"CremacionId\" IS NULL AND \"CremacionActivaSnapshot\" IS NULL AND \"EstadoCremacionSnapshot\" IS NULL) OR (\"CremacionId\" IS NOT NULL AND \"CremacionActivaSnapshot\" IS NOT NULL AND \"EstadoCremacionSnapshot\" IS NOT NULL)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_CuentaPagoSnapshot", "(\"CuentaPagoId\" IS NULL AND \"TotalServicioSnapshot\" IS NULL AND \"MontoPagadoSnapshot\" IS NULL AND \"MontoPagoRequeridoRecoleccionSnapshot\" IS NULL AND \"RequiereRevisionFinancieraSnapshot\" IS NULL AND \"CantidadPagosSnapshot\" = 0) OR (\"CuentaPagoId\" IS NOT NULL AND \"TotalServicioSnapshot\" IS NOT NULL AND \"MontoPagadoSnapshot\" IS NOT NULL AND \"RequiereRevisionFinancieraSnapshot\" IS NOT NULL)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_Dependencias", "\"DependenciasSnapshot\" >= 0 AND \"DependenciasSnapshot\" <= 127");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_DependenciasConsistentes", "(((\"DependenciasSnapshot\" & 1) <> 0) = (\"RecoleccionId\" IS NOT NULL)) AND (((\"DependenciasSnapshot\" & 2) <> 0) = \"TieneSolicitudVeterinariaConvertidaSnapshot\") AND (((\"DependenciasSnapshot\" & 4) <> 0) = (\"CremacionId\" IS NOT NULL)) AND (((\"DependenciasSnapshot\" & 8) <> 0) = (\"CuentaPagoId\" IS NOT NULL)) AND (((\"DependenciasSnapshot\" & 16) <> 0) = (\"CantidadPagosSnapshot\" > 0)) AND (((\"DependenciasSnapshot\" & 32) <> 0) = (\"CantidadEvidenciasRecepcionActivasSnapshot\" > 0)) AND (((\"DependenciasSnapshot\" & 64) <> 0) = (\"CantidadEvidenciasRecoleccionSnapshot\" > 0 OR \"CantidadHistorialAsignacionesRecoleccionSnapshot\" > 0))");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_EstadoCremacion", "\"EstadoCremacionSnapshot\" IS NULL OR \"EstadoCremacionSnapshot\" IN (1, 2, 3, 4, 5, 6, 7, 8, 9)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_EstadoRecoleccion", "\"EstadoRecoleccionSnapshot\" IS NULL OR \"EstadoRecoleccionSnapshot\" IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_EtapaCremacion", "(\"CremacionId\" IS NULL AND \"EtapaOperativaSnapshot\" = 1) OR (\"EstadoCremacionSnapshot\" IN (1, 2, 9) AND \"EtapaOperativaSnapshot\" = 2) OR (\"EstadoCremacionSnapshot\" IN (3, 4, 5) AND \"EtapaOperativaSnapshot\" = 3) OR (\"EstadoCremacionSnapshot\" IN (6, 7, 8) AND \"EtapaOperativaSnapshot\" = 4)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_EtapaOperativa", "\"EtapaOperativaSnapshot\" IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_Motivo", "NULLIF(BTRIM(\"Motivo\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_MotivoBloqueo", "(\"TipoResultado\" = 3 AND \"MotivoBloqueoSnapshot\" IN (1, 2)) OR (\"TipoResultado\" <> 3 AND \"MotivoBloqueoSnapshot\" = 0)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_NumeroSecuencia", "\"NumeroSecuencia\" > 0");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_RecoleccionSnapshot", "(\"RecoleccionId\" IS NULL AND \"RecoleccionActivaSnapshot\" IS NULL AND \"EstadoRecoleccionSnapshot\" IS NULL AND \"FechaRecoleccionSnapshot\" IS NULL AND \"FechaRecepcionRecoleccionSnapshot\" IS NULL) OR (\"RecoleccionId\" IS NOT NULL AND \"RecoleccionActivaSnapshot\" IS NOT NULL AND \"EstadoRecoleccionSnapshot\" IS NOT NULL)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_SolicitudVeterinariaSnapshot", "(\"TieneSolicitudVeterinariaConvertidaSnapshot\" = FALSE AND \"SolicitudVeterinariaId\" IS NULL) OR (\"TieneSolicitudVeterinariaConvertidaSnapshot\" = TRUE AND \"SolicitudVeterinariaId\" IS NOT NULL)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_TipoAccion", "\"TipoAccion\" IN (1, 2, 3)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_TipoResultado", "\"TipoResultado\" IN (1, 2, 3)");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_TransicionActivo", "(\"TipoAccion\" = 1 AND \"RecepcionActivaAnterior\" = \"RecepcionActivaNueva\") OR (\"TipoAccion\" = 2 AND \"TipoResultado\" = 2 AND \"RecepcionActivaAnterior\" = TRUE AND \"RecepcionActivaNueva\" = FALSE) OR (\"TipoAccion\" = 2 AND \"TipoResultado\" = 3 AND \"RecepcionActivaAnterior\" = \"RecepcionActivaNueva\") OR (\"TipoAccion\" = 3 AND \"TipoResultado\" = 2 AND \"RecepcionActivaAnterior\" = FALSE AND \"RecepcionActivaNueva\" = TRUE) OR (\"TipoAccion\" = 3 AND \"TipoResultado\" = 3 AND \"RecepcionActivaAnterior\" = \"RecepcionActivaNueva\")");
                    table.CheckConstraint("CK_EventosCicloVidaRecepcion_ValoresNoNegativos", "\"CantidadPagosSnapshot\" >= 0 AND \"CantidadEvidenciasRecepcionActivasSnapshot\" >= 0 AND \"CantidadEvidenciasRecoleccionSnapshot\" >= 0 AND \"CantidadHistorialAsignacionesRecoleccionSnapshot\" >= 0 AND \"CantidadHistorialRecepcionSnapshot\" >= 0 AND (\"TotalServicioSnapshot\" IS NULL OR \"TotalServicioSnapshot\" >= 0) AND (\"MontoPagadoSnapshot\" IS NULL OR \"MontoPagadoSnapshot\" >= 0) AND (\"MontoPagoRequeridoRecoleccionSnapshot\" IS NULL OR \"MontoPagoRequeridoRecoleccionSnapshot\" >= 0)");
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_Cremaciones_CremacionId",
                        column: x => x.CremacionId,
                        principalTable: "Cremaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_CuentasPago_CuentaPagoId",
                        column: x => x.CuentaPagoId,
                        principalTable: "CuentasPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_Recepciones_RecepcionId",
                        column: x => x.RecepcionId,
                        principalTable: "Recepciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_Recolecciones_RecoleccionId",
                        column: x => x.RecoleccionId,
                        principalTable: "Recolecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_SolicitudesVeterinarias_Solicitud~",
                        column: x => x.SolicitudVeterinariaId,
                        principalTable: "SolicitudesVeterinarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCicloVidaRecepcion_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_CreadoPorUsuarioId",
                table: "EventosCicloVidaRecepcion",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_CremacionId",
                table: "EventosCicloVidaRecepcion",
                column: "CremacionId",
                filter: "\"CremacionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_CuentaPagoId",
                table: "EventosCicloVidaRecepcion",
                column: "CuentaPagoId",
                filter: "\"CuentaPagoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_RecepcionId_FechaCreacion_Id",
                table: "EventosCicloVidaRecepcion",
                columns: new[] { "RecepcionId", "FechaCreacion", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_RecepcionId_NumeroSecuencia",
                table: "EventosCicloVidaRecepcion",
                columns: new[] { "RecepcionId", "NumeroSecuencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_RecepcionId_SolicitudId",
                table: "EventosCicloVidaRecepcion",
                columns: new[] { "RecepcionId", "SolicitudId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_RecoleccionId",
                table: "EventosCicloVidaRecepcion",
                column: "RecoleccionId",
                filter: "\"RecoleccionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCicloVidaRecepcion_SolicitudVeterinariaId",
                table: "EventosCicloVidaRecepcion",
                column: "SolicitudVeterinariaId",
                filter: "\"SolicitudVeterinariaId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosCicloVidaRecepcion");
        }
    }
}
