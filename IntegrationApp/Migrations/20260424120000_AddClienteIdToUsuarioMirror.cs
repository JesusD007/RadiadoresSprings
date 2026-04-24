using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteIdToUsuarioMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "UsuarioMirror",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "UsuarioMirror");
        }
    }
}
