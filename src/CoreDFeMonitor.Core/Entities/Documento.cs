using System;

namespace CoreDFeMonitor.Core.Entities
{
    public class Documento
    {
        public Guid Id { get; set; }
        public string ChaveAcesso { get; set; } = string.Empty;
        public string Nsu { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = "NFe";
        public string TipoEvento { get; set; } = string.Empty;
        public string NomeEvento { get; set; } = string.Empty;

        public string Schema { get; set; } = string.Empty;
        public string XmlConteudo { get; set; } = string.Empty;

        public DateTimeOffset DataEmissao { get; set; }
        public DateTimeOffset DataProcessamento { get; set; }
        public bool CienciaEnviada { get; set; }

        // Mantém o vínculo com a sua Empresa (dona do certificado)
        public Guid EmpresaId { get; set; }

        // NOVO: Vínculo com o Emitente (Fornecedor)
        public int EmitenteId { get; set; }
        public Emitente Emitente { get; set; } = null!;

        public Documento(Guid empresaId, string nsu, string schema, string xmlConteudo)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId;
            Nsu = nsu;
            Schema = schema;
            XmlConteudo = xmlConteudo;
            DataProcessamento = DateTimeOffset.Now;
            DataEmissao = DateTimeOffset.Now;
        }

        public void MarcarCienciaEnviada()
        {
            CienciaEnviada = true;
        }

        public bool RequerCienciaAutomatica(string cnpjEmpresa)
        {
            return Schema.Contains("resNFe") && !CienciaEnviada;
        }
    }
}