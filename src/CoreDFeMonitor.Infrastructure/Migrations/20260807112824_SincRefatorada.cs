using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreDFeMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SincRefatorada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UltimaConsultaVazia",
                table: "Empresas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UltimaConsultaVazia",
                table: "Empresas");
        }
    }
}
