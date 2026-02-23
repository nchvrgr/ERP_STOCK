using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servidor.Infraestructura.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Servidor.Infraestructura.Persistence.PosDbContext))]
[Migration("20260209190000_AddStockDeseado")]
public partial class AddStockDeseado : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "stockdeseado",
            table: "producto_stock_config",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "stockdeseado",
            table: "producto_stock_config");
    }
}

