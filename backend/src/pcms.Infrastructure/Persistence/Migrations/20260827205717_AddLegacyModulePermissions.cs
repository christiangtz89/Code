using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyModulePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("00000010-0000-0000-0000-000000000001"), "Customers.View", "Consultar clientes" },
                    { new Guid("00000011-0000-0000-0000-000000000001"), "Customers.Manage", "Administrar clientes" },
                    { new Guid("00000012-0000-0000-0000-000000000001"), "Pets.View", "Consultar mascotas" },
                    { new Guid("00000013-0000-0000-0000-000000000001"), "Pets.Manage", "Administrar mascotas" },
                    { new Guid("00000014-0000-0000-0000-000000000001"), "VeterinaryClinics.View", "Consultar veterinarias" },
                    { new Guid("00000015-0000-0000-0000-000000000001"), "VeterinaryClinics.Manage", "Administrar veterinarias" },
                    { new Guid("00000016-0000-0000-0000-000000000001"), "Veterinarians.View", "Consultar veterinarios" },
                    { new Guid("00000017-0000-0000-0000-000000000001"), "Veterinarians.Manage", "Administrar veterinarios" },
                    { new Guid("00000018-0000-0000-0000-000000000001"), "VeterinaryRequests.View", "Consultar solicitudes veterinarias" },
                    { new Guid("00000019-0000-0000-0000-000000000001"), "VeterinaryRequests.Manage", "Administrar solicitudes veterinarias" },
                    { new Guid("00000020-0000-0000-0000-000000000001"), "Collections.View", "Consultar recolecciones" },
                    { new Guid("00000021-0000-0000-0000-000000000001"), "Collections.Manage", "Administrar recolecciones" },
                    { new Guid("00000022-0000-0000-0000-000000000001"), "Receptions.View", "Consultar recepciones" },
                    { new Guid("00000023-0000-0000-0000-000000000001"), "Receptions.Manage", "Administrar recepciones" },
                    { new Guid("00000024-0000-0000-0000-000000000001"), "Cremations.View", "Consultar cremaciones" },
                    { new Guid("00000025-0000-0000-0000-000000000001"), "Cremations.Manage", "Administrar cremaciones" },
                    { new Guid("00000026-0000-0000-0000-000000000001"), "Payments.View", "Consultar pagos" },
                    { new Guid("00000027-0000-0000-0000-000000000001"), "Payments.Manage", "Administrar pagos" },
                    { new Guid("00000028-0000-0000-0000-000000000001"), "CremationPackages.View", "Consultar paquetes de cremación" },
                    { new Guid("00000029-0000-0000-0000-000000000001"), "CremationPackages.Manage", "Administrar paquetes de cremación" },
                    { new Guid("00000030-0000-0000-0000-000000000001"), "Urns.View", "Consultar urnas" },
                    { new Guid("00000031-0000-0000-0000-000000000001"), "Urns.Manage", "Administrar urnas" },
                    { new Guid("00000032-0000-0000-0000-000000000001"), "CremationPricing.View", "Consultar precios de cremación" },
                    { new Guid("00000033-0000-0000-0000-000000000001"), "CremationPricing.Manage", "Administrar precios de cremación" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000010-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000011-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000012-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000013-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000014-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000015-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000016-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000017-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000018-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000019-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000020-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000021-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000022-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000023-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000024-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000025-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000026-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000027-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000028-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000029-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000030-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000031-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000032-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000033-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000010-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000011-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000012-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000013-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000014-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000015-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000016-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000017-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000018-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000019-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000020-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000021-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000022-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000023-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000024-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000025-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000026-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000027-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000028-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000029-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000030-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000031-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000032-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolPermisos",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000033-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000010-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000011-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000012-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000013-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000014-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000015-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000016-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000017-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000018-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000019-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000020-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000021-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000022-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000023-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000024-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000025-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000026-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000027-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000028-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000029-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000030-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000031-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000032-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("00000033-0000-0000-0000-000000000001"));
        }
    }
}
