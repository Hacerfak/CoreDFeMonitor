using System.Text.RegularExpressions;

namespace CoreDFeMonitor.Core.Entities
{
    public class Documento
    {
        public Guid Id { get; private set; }
        public Guid EmpresaId { get; private set; }
        public string Nsu { get; private set; } = string.Empty;
        public string ChaveAcesso { get; private set; } = string.Empty;
        public string Schema { get; private set; } = string.Empty;
        public string XmlConteudo { get; private set; } = string.Empty;
        public DateTime DataProcessamento { get; private set; }

        public bool CienciaEnviada { get; private set; } = false;

        protected Documento() { }

        public Documento(Guid empresaId, string nsu, string schema, string xmlConteudo)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId;
            Nsu = nsu.PadLeft(15, '0');
            Schema = schema;
            XmlConteudo = xmlConteudo;
            DataProcessamento = DateTime.UtcNow;
            ChaveAcesso = ExtrairChave(xmlConteudo);
        }

        public void MarcarCienciaEnviada()
        {
            CienciaEnviada = true;
        }

        // Determina se este XML é uma NFe que precisa do Evento 210210
        public bool RequerCienciaAutomatica(string cnpjDaNossaEmpresa)
        {
            // Eventos de cancelamento ou correções da Sefaz não recebem Ciência
            if (Schema.StartsWith("resEvento") || Schema.StartsWith("procEvento") || Schema.StartsWith("retEnvEvento"))
                return false;

            if (ChaveAcesso == "SEM_CHAVE_NO_RESUMO" || ChaveAcesso.Length != 44)
                return false;

            // Extraímos a primeira tag CNPJ/CPF do XML, que indica o Emitente
            var match = Regex.Match(XmlConteudo, @"<(CNPJ|CPF)>([0-9]+)</\1>");
            if (match.Success)
            {
                string docEmitente = match.Groups[2].Value;

                // Se o emitente for a própria Empresa (ex: Emissão própria), não damos Ciência.
                if (docEmitente == cnpjDaNossaEmpresa)
                    return false;
            }

            return true;
        }

        private string ExtrairChave(string xml)
        {
            var match = Regex.Match(xml, @"<chNFe>([0-9]{44})</chNFe>");
            return match.Success ? match.Groups[1].Value : "SEM_CHAVE_NO_RESUMO";
        }
    }
}