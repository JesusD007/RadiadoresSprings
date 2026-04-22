using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClienteMirror",
                columns: table => new
                {
                    LocalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoreId = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    RFC = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Regular"),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    EsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteMirror", x => x.LocalId);
                });

            migrationBuilder.CreateTable(
                name: "OperacionPendiente",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEntidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TipoOperacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EndpointCore = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MetodoHttp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "POST"),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    IdLocalTemporal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UsuarioId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntentosSync = table.Column<int>(type: "integer", nullable: false),
                    UltimoIntento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RespuestaCore = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionPendiente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SesionCajaMirror",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdLocal = table.Column<Guid>(type: "uuid", nullable: false),
                    CajaId = table.Column<int>(type: "integer", nullable: false),
                    NombreCaja = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    NombreUsuario = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MontoApertura = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoCierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Abierta"),
                    FechaApertura = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstadoSync = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    CoreSesionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionCajaMirror", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioMirror",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Rol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioMirror", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteMirror_CoreId",
                table: "ClienteMirror",
                column: "CoreId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionPendiente_Estado",
                table: "OperacionPendiente",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionPendiente_FechaCreacion",
                table: "OperacionPendiente",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionPendiente_IdempotencyKey",
                table: "OperacionPendiente",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionCajaMirror_EstadoSync",
                table: "SesionCajaMirror",
                column: "EstadoSync");

            migrationBuilder.CreateIndex(
                name: "IX_SesionCajaMirror_IdLocal",
                table: "SesionCajaMirror",
                column: "IdLocal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioMirror_Username",
                table: "UsuarioMirror",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteMirror");

            migrationBuilder.DropTable(
                name: "OperacionPendiente");

            migrationBuilder.DropTable(
                name: "SesionCajaMirror");

            migrationBuilder.DropTable(
                name: "UsuarioMirror");
        }
    }
}
