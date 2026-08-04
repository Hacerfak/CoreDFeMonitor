using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreDFeMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emitentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    RazaoSocial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emitentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    RazaoSocial = table.Column<string>(type: "TEXT", nullable: false),
                    Uf = table.Column<string>(type: "TEXT", nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "TEXT", nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Logradouro = table.Column<string>(type: "TEXT", nullable: true),
                    Numero = table.Column<string>(type: "TEXT", nullable: true),
                    Complemento = table.Column<string>(type: "TEXT", nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", nullable: true),
                    CodigoMunicipio = table.Column<long>(type: "INTEGER", nullable: true),
                    NomeMunicipio = table.Column<string>(type: "TEXT", nullable: true),
                    Cep = table.Column<string>(type: "TEXT", nullable: true),
                    CaminhoCertificado = table.Column<string>(type: "TEXT", nullable: true),
                    SenhaCertificado = table.Column<string>(type: "TEXT", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoNsu = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false, defaultValue: "000000000000000")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    Nsu = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    TipoDocumento = table.Column<string>(type: "TEXT", nullable: false),
                    TipoEvento = table.Column<string>(type: "TEXT", nullable: false),
                    NomeEvento = table.Column<string>(type: "TEXT", nullable: false),
                    Schema = table.Column<string>(type: "TEXT", nullable: false),
                    XmlConteudo = table.Column<string>(type: "TEXT", nullable: false),
                    DataEmissao = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataProcessamento = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CienciaEnviada = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmitenteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Emitentes_EmitenteId",
                        column: x => x.EmitenteId,
                        principalTable: "Emitentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmitenteId",
                table: "Documentos",
                column: "EmitenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_EmpresaId_Nsu",
                table: "Documentos",
                columns: new[] { "EmpresaId", "Nsu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cnpj",
                table: "Empresas",
                column: "Cnpj",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Emitentes");
        }
    }
}
