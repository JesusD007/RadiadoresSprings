using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMirrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CajaMirror",
                columns: table => new
                {
                    CoreId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaMirror", x => x.CoreId);
                });

            migrationBuilder.CreateTable(
                name: "CategoriaMirror",
                columns: table => new
                {
                    CoreId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaMirror", x => x.CoreId);
                });

            migrationBuilder.CreateTable(
                name: "CuentaCobrarMirror",
                columns: table => new
                {
                    CoreId = table.Column<int>(type: "integer", nullable: false),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentaCobrarMirror", x => x.CoreId);
                });

            migrationBuilder.CreateTable(
                name: "SucursalMirror",
                columns: table => new
                {
                    CoreId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SucursalMirror", x => x.CoreId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CajaMirror_SucursalId",
                table: "CajaMirror",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaCobrarMirror_ClienteId",
                table: "CuentaCobrarMirror",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CajaMirror");

            migrationBuilder.DropTable(
                name: "CategoriaMirror");

            migrationBuilder.DropTable(
                name: "CuentaCobrarMirror");

            migrationBuilder.DropTable(
                name: "SucursalMirror");
        }
    }
}
