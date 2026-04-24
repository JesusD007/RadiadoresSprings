using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCajaIdToVentaOffline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CajaId",
                table: "VentaOfflinePendiente",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "VentaOfflinePendiente",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "VentaOfflinePendiente",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SesionCajaId",
                table: "VentaOfflinePendiente",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CajaId",
                table: "VentaOfflinePendiente");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "VentaOfflinePendiente");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "VentaOfflinePendiente");

            migrationBuilder.DropColumn(
                name: "SesionCajaId",
                table: "VentaOfflinePendiente");
        }
    }
}
