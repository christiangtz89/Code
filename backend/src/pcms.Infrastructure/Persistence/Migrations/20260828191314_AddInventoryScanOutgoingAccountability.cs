using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryScanOutgoingAccountability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_RecordedByUserId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientOperationId",
                table: "MovimientosInventarioInsumos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CremationId",
                table: "MovimientosInventarioInsumos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "MovimientosInventarioInsumos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReasonCode",
                table: "MovimientosInventarioInsumos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordedByDisplayNameSnapshot",
                table: "MovimientosInventarioInsumos",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannedCode",
                table: "MovimientosInventarioInsumos",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplyItemNameSnapshot",
                table: "MovimientosInventarioInsumos",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "MovimientosInventarioInsumos" AS movement
                        LEFT JOIN "Insumos" AS item ON item."Id" = movement."SupplyItemId"
                        WHERE item."Id" IS NULL
                           OR item."Name" IS NULL
                           OR btrim(item."Name") = ''
                           OR length(item."Name") > 250
                    ) THEN
                        RAISE EXCEPTION 'Inventory movement item-name snapshots cannot be backfilled safely.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "MovimientosInventarioInsumos" AS movement
                        INNER JOIN "Usuarios" AS employee ON employee."Id" = movement."RecordedByUserId"
                        WHERE length(btrim(concat_ws(' ', employee."Nombre", employee."Apellido"))) > 250
                    ) THEN
                        RAISE EXCEPTION 'Inventory movement employee-name snapshots exceed 250 characters.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                UPDATE "MovimientosInventarioInsumos" AS movement
                SET "SupplyItemNameSnapshot" = item."Name"
                FROM "Insumos" AS item
                WHERE item."Id" = movement."SupplyItemId";
                """);

            migrationBuilder.Sql("""
                UPDATE "MovimientosInventarioInsumos" AS movement
                SET "RecordedByDisplayNameSnapshot" =
                    NULLIF(btrim(concat_ws(' ', employee."Nombre", employee."Apellido")), '')
                FROM "Usuarios" AS employee
                WHERE employee."Id" = movement."RecordedByUserId";
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        WITH cremation_links AS (
                            SELECT fulfillment."UrnMovementId" AS movement_id,
                                   fulfillment."CremationId" AS cremation_id
                            FROM "EntregasInventarioCremacion" AS fulfillment
                            UNION ALL
                            SELECT material."InventoryMovementId" AS movement_id,
                                   fulfillment."CremationId" AS cremation_id
                            FROM "MaterialesEntregaCremacion" AS material
                            INNER JOIN "EntregasInventarioCremacion" AS fulfillment
                                ON fulfillment."Id" = material."FulfillmentId"
                        )
                        SELECT 1
                        FROM cremation_links
                        GROUP BY movement_id
                        HAVING COUNT(DISTINCT cremation_id) > 1
                    ) THEN
                        RAISE EXCEPTION 'A movement is linked to more than one cremation.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                UPDATE "MovimientosInventarioInsumos" AS movement
                SET "CremationId" = fulfillment."CremationId"
                FROM "EntregasInventarioCremacion" AS fulfillment
                WHERE fulfillment."UrnMovementId" = movement."Id";

                UPDATE "MovimientosInventarioInsumos" AS movement
                SET "CremationId" = fulfillment."CremationId"
                FROM "MaterialesEntregaCremacion" AS material
                INNER JOIN "EntregasInventarioCremacion" AS fulfillment
                    ON fulfillment."Id" = material."FulfillmentId"
                WHERE material."InventoryMovementId" = movement."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "MovimientosInventarioInsumos"
                SET "Origin" = CASE
                    WHEN "CremationId" IS NOT NULL THEN 2
                    WHEN "MovementType" IN (6, 8) THEN 3
                    WHEN "MovementType" = 1 THEN 4
                    ELSE 5
                END;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "MovimientosInventarioInsumos"
                        WHERE "SupplyItemNameSnapshot" IS NULL
                           OR btrim("SupplyItemNameSnapshot") = ''
                           OR "Origin" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Inventory movement accountability backfill is incomplete.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Origin",
                table: "MovimientosInventarioInsumos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SupplyItemNameSnapshot",
                table: "MovimientosInventarioInsumos",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[] { new Guid("00000034-0000-0000-0000-000000000001"), "Inventory.ScanOutgoing", "Registrar salidas por escaneo" });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("00000034-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_ClientOperationId",
                table: "MovimientosInventarioInsumos",
                column: "ClientOperationId",
                unique: true,
                filter: "\"ClientOperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_CremationId",
                table: "MovimientosInventarioInsumos",
                column: "CremationId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_Origin_OccurredAt",
                table: "MovimientosInventarioInsumos",
                columns: new[] { "Origin", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_RecordedByUserId_OccurredAt",
                table: "MovimientosInventarioInsumos",
                columns: new[] { "RecordedByUserId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventarioInsumos_Cremaciones_CremationId",
                table: "MovimientosInventarioInsumos",
                column: "CremationId",
                principalTable: "Cremaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventarioInsumos_Cremaciones_CremationId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_ClientOperationId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_CremationId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_Origin_OccurredAt",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventarioInsumos_RecordedByUserId_OccurredAt",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000034-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "CremationId",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "RecordedByDisplayNameSnapshot",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "ScannedCode",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.DropColumn(
                name: "SupplyItemNameSnapshot",
                table: "MovimientosInventarioInsumos");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventarioInsumos_RecordedByUserId",
                table: "MovimientosInventarioInsumos",
                column: "RecordedByUserId");
        }
    }
}
