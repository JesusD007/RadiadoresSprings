using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTiposDeDatosInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""VentaOfflinePendiente"" ALTER COLUMN ""SucursalId"" TYPE integer USING 0;
                ALTER TABLE ""VentaOfflinePendiente"" ALTER COLUMN ""ClienteId"" TYPE integer USING 0;
                ALTER TABLE ""VentaOfflinePendiente"" ALTER COLUMN ""CajeroId"" TYPE integer USING 0;
                ALTER TABLE ""SesionCajaMirror"" ALTER COLUMN ""IdLocal"" TYPE integer USING 0;
                ALTER TABLE ""IdempotencyLog"" ALTER COLUMN ""FacturaIdCore"" TYPE integer USING NULL;
                ALTER TABLE ""ClienteMirror"" ALTER COLUMN ""LocalId"" TYPE integer USING 0;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "SucursalId",
                table: "VentaOfflinePendiente",
                type: "integer",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "ClienteId",
                table: "VentaOfflinePendiente",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "CajeroId",
                table: "VentaOfflinePendiente",
                type: "integer",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "IdLocal",
                table: "SesionCajaMirror",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "FacturaIdCore",
                table: "IdempotencyLog",
                type: "integer",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LocalId",
                table: "ClienteMirror",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SucursalId",
                table: "VentaOfflinePendiente",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClienteId",
                table: "VentaOfflinePendiente",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CajeroId",
                table: "VentaOfflinePendiente",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "IdLocal",
                table: "SesionCajaMirror",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "FacturaIdCore",
                table: "IdempotencyLog",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocalId",
                table: "ClienteMirror",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
