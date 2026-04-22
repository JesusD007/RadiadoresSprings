using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── UsuarioMirror ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "UsuarioMirror",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false),
                    Username     = table.Column<string>(maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(maxLength: 500, nullable: false),
                    Rol          = table.Column<string>(maxLength: 50, nullable: false),
                    Nombre       = table.Column<string>(maxLength: 100, nullable: false),
                    Apellido     = table.Column<string>(maxLength: 100, nullable: true),
                    Email        = table.Column<string>(maxLength: 200, nullable: true),
                    EsActivo     = table.Column<bool>(nullable: false, defaultValue: true),
                    UltimaSync   = table.Column<DateTime>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_UsuarioMirror", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioMirror_Username",
                table: "UsuarioMirror",
                column: "Username",
                unique: true);

            // ── ClienteMirror ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ClienteMirror",
                columns: table => new
                {
                    LocalId       = table.Column<Guid>(nullable: false),
                    CoreId        = table.Column<int>(nullable: true),
                    Nombre        = table.Column<string>(maxLength: 150, nullable: false),
                    Apellido      = table.Column<string>(maxLength: 150, nullable: true),
                    Email         = table.Column<string>(maxLength: 200, nullable: true),
                    Telefono      = table.Column<string>(maxLength: 30, nullable: true),
                    Direccion     = table.Column<string>(nullable: true),
                    RFC           = table.Column<string>(maxLength: 20, nullable: true),
                    Tipo          = table.Column<string>(maxLength: 30, nullable: false, defaultValue: "Regular"),
                    LimiteCredito = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                    SaldoPendiente= table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                    EsActivo      = table.Column<bool>(nullable: false, defaultValue: true),
                    EsLocal       = table.Column<bool>(nullable: false, defaultValue: false),
                    UltimaSync    = table.Column<DateTime>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ClienteMirror", x => x.LocalId));

            migrationBuilder.CreateIndex(
                name: "IX_ClienteMirror_CoreId",
                table: "ClienteMirror",
                column: "CoreId");

            // ── SesionCajaMirror ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "SesionCajaMirror",
                columns: table => new
                {
                    Id            = table.Column<long>(nullable: false)
                                         .Annotation("Npgsql:ValueGenerationStrategy",
                                             NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdLocal       = table.Column<Guid>(nullable: false),
                    CajaId        = table.Column<int>(nullable: false),
                    NombreCaja    = table.Column<string>(maxLength: 100, nullable: false),
                    UsuarioId     = table.Column<int>(nullable: false),
                    NombreUsuario = table.Column<string>(maxLength: 200, nullable: false),
                    MontoApertura = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                    MontoCierre   = table.Column<decimal>(precision: 18, scale: 2, nullable: true),
                    Estado        = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Abierta"),
                    FechaApertura = table.Column<DateTimeOffset>(nullable: false),
                    FechaCierre   = table.Column<DateTimeOffset>(nullable: true),
                    Observaciones = table.Column<string>(maxLength: 500, nullable: true),
                    EstadoSync    = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    CoreSesionId  = table.Column<int>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SesionCajaMirror", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_SesionCajaMirror_IdLocal",
                table: "SesionCajaMirror",
                column: "IdLocal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionCajaMirror_EstadoSync",
                table: "SesionCajaMirror",
                column: "EstadoSync");

            // ── OperacionPendiente ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "OperacionPendiente",
                columns: table => new
                {
                    Id               = table.Column<long>(nullable: false)
                                            .Annotation("Npgsql:ValueGenerationStrategy",
                                                NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdempotencyKey   = table.Column<Guid>(nullable: false),
                    TipoEntidad      = table.Column<string>(maxLength: 50, nullable: false),
                    TipoOperacion    = table.Column<string>(maxLength: 50, nullable: false),
                    EndpointCore     = table.Column<string>(maxLength: 300, nullable: false),
                    MetodoHttp       = table.Column<string>(maxLength: 10, nullable: false, defaultValue: "POST"),
                    PayloadJson      = table.Column<string>(type: "text", nullable: false),
                    IdLocalTemporal  = table.Column<string>(maxLength: 100, nullable: true),
                    UsuarioId        = table.Column<string>(maxLength: 100, nullable: true),
                    Estado           = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion    = table.Column<DateTimeOffset>(nullable: false),
                    IntentosSync     = table.Column<int>(nullable: false, defaultValue: 0),
                    UltimoIntento    = table.Column<DateTimeOffset>(nullable: true),
                    MotivoRechazo    = table.Column<string>(maxLength: 1000, nullable: true),
                    RespuestaCore    = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OperacionPendiente", x => x.Id));

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OperacionPendiente");
            migrationBuilder.DropTable(name: "SesionCajaMirror");
            migrationBuilder.DropTable(name: "ClienteMirror");
            migrationBuilder.DropTable(name: "UsuarioMirror");
        }
    }
}
