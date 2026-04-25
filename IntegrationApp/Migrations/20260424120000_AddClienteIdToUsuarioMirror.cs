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
            // No-op: ClienteId se resuelve por email en runtime (sin columna en UsuarioMirror).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
