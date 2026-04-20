using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTransaccionLocal = table.Column<Guid>(type: "uuid", nullable: false),
                    FacturaIdCore = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaConfirmacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    HttpStatus = table.Column<int>(type: "integer", nullable: false),
                    LatenciaMs = table.Column<int>(type: "integer", nullable: false),
                    DesdeCache = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Layer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Integracion"),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoMirror",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrecioOferta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StockMinimo = table.Column<int>(type: "integer", nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdTransaccionLocal = table.Column<Guid>(type: "uuid", nullable: false),
                    CajeroId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SucursalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoRecibido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineasJson = table.Column<string>(type: "text", nullable: false),
                    FechaLocal = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    IntentosSync = table.Column<int>(type: "integer", nullable: false),
                    UltimoIntento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
