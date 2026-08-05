using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreDFeMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaManifestacaoImpressao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoManifestacao",
                table: "Documentos",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoManifestacao",
                table: "Documentos");
        }
    }
}
