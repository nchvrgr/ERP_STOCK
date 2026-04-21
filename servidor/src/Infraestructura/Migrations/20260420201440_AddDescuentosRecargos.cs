using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servidor.Infraestructura.Migrations
{
    public partial class AddDescuentosRecargos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "descuentos_recargos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_descuentos_recargos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_descuentos_recargos_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_descuentos_recargos_TenantId_Tipo_Name",
                table: "descuentos_recargos",
                columns: new[] { "TenantId", "Tipo", "Name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "descuentos_recargos");
        }
    }
}
