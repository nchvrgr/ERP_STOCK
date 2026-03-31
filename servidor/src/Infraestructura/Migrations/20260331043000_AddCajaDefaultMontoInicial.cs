using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servidor.Infraestructura.Migrations
{
    public partial class AddCajaDefaultMontoInicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultMontoInicial",
                table: "cajas",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMontoInicial",
                table: "cajas");
        }
    }
}
