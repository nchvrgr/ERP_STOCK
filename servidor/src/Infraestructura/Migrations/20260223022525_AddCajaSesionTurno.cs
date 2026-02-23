using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servidor.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddCajaSesionTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "turno",
                table: "caja_sesiones",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "MANANA");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "turno",
                table: "caja_sesiones");
        }
    }
}
