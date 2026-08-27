using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace pcms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurablePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), "Permissions.Manage", "Administrar permisos" },
                    { new Guid("00000002-0000-0000-0000-000000000001"), "Suppliers.View", "Consultar proveedores" },
                    { new Guid("00000003-0000-0000-0000-000000000001"), "Suppliers.Manage", "Administrar proveedores" },
                    { new Guid("00000004-0000-0000-0000-000000000001"), "Finance.View", "Consultar finanzas" },
                    { new Guid("00000005-0000-0000-0000-000000000001"), "Finance.Manage", "Administrar finanzas" },
                    { new Guid("00000006-0000-0000-0000-000000000001"), "Inventory.View", "Consultar inventario" },
                    { new Guid("00000007-0000-0000-0000-000000000001"), "Inventory.Manage", "Administrar inventario" },
                    { new Guid("00000008-0000-0000-0000-000000000001"), "Purchasing.View", "Consultar compras" },
                    { new Guid("00000009-0000-0000-0000-000000000001"), "Purchasing.Manage", "Administrar compras" }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000002-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000003-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000004-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000005-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000006-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000007-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000008-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("00000009-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Code",
                table: "Permisos",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermissionId",
                table: "RolPermisos",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "Permisos");
        }
    }
}
