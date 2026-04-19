using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTransaccionLocal = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacturaIdCore = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaConfirmacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpStatus = table.Column<int>(type: "int", nullable: false),
                    LatenciaMs = table.Column<int>(type: "int", nullable: false),
                    DesdeCache = table.Column<bool>(type: "bit", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Layer = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Integracion"),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoMirror",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrecioOferta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StockMinimo = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoMirror", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VentaOfflinePendiente",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTransaccionLocal = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CajeroId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SucursalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoRecibido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineasJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    IntentosSync = table.Column<int>(type: "int", nullable: false),
                    UltimoIntento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaOfflinePendiente", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyLog_IdTransaccionLocal",
                table: "IdempotencyLog",
                column: "IdTransaccionLocal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLog_CorrelationId",
                table: "IntegrationLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLog_Fecha",
                table: "IntegrationLog",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_VentaOfflinePendiente_Estado",
                table: "VentaOfflinePendiente",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_VentaOfflinePendiente_IdTransaccionLocal",
                table: "VentaOfflinePendiente",
                column: "IdTransaccionLocal",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyLog");

            migrationBuilder.DropTable(
                name: "IntegrationLog");

            migrationBuilder.DropTable(
                name: "ProductoMirror");

            migrationBuilder.DropTable(
                name: "VentaOfflinePendiente");
        }
    }
}
