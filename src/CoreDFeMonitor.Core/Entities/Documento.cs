using System.Text.RegularExpressions;

namespace CoreDFeMonitor.Core.Entities
{
    public class Documento
    {
        public Guid Id { get; private set; }
        public Guid EmpresaId { get; private set; }
        public string Nsu { get; private set; } = string.Empty;
        public string Schema { get; private set; } = string.Empty;
        public string XmlConteudo { get; private set; } = string.Empty;
        public string ChaveAcesso { get; private set; } = string.Empty;
        public bool CienciaEnviada { get; private set; }
        public DateTimeOffset DataProcessamento { get; private set; }
        public DateTimeOffset DataEmissao { get; private set; }
        public string TipoDocumento { get; private set; } = "Desconhecido";
        public string TipoEvento { get; private set; } = string.Empty;
        public string NomeEvento { get; private set; } = string.Empty;

        protected Documento() { }

        public Documento(Guid empresaId, string nsu, string schema, string xmlConteudo)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId;
            Nsu = nsu;
            Schema = schema;
            XmlConteudo = xmlConteudo;

            // Define a data de importação como o EXATO MOMENTO da criação
            DataProcessamento = DateTimeOffset.UtcNow;
            CienciaEnviada = false;

            ProcessarMetadadosDoXml();
        }

        private void ProcessarMetadadosDoXml()
        {
            var matchChave = Regex.Match(XmlConteudo, @"<ch(?:NFe|CTe)>([0-9]{44})</ch(?:NFe|CTe)>");
            ChaveAcesso = matchChave.Success ? matchChave.Groups[1].Value : "SEM_CHAVE_NO_RESUMO";

            // === EXTRAÇÃO DA DATA DE EMISSÃO ===
            // Busca dhEmi (Emissão de NFe/CTe) ou dhEvento (Data do Evento) ou dhRecbto (Data do Resumo)
            var matchData = Regex.Match(XmlConteudo, @"<(?:dhEmi|dhEvento|dhRecbto)>(.*?)</(?:dhEmi|dhEvento|dhRecbto)>");
            if (matchData.Success && DateTimeOffset.TryParse(matchData.Groups[1].Value, out var dhParsed))
            {
                // Agora guardamos o DateTimeOffset exato do XML (Ex: 2026-03-30 10:36:31 -03:00)
                DataEmissao = dhParsed;
            }
            else
            {
                DataEmissao = DataProcessamento;
            }

            // Classificação inteligente
            if (Schema.Contains("procNFe")) TipoDocumento = "NF-e";
            else if (Schema.Contains("resNFe")) TipoDocumento = "Resumo NF-e";
            else if (Schema.Contains("procCTe")) TipoDocumento = "CT-e";
            else if (Schema.Contains("resCTe")) TipoDocumento = "Resumo CT-e";
            else if (Schema.Contains("Evento", StringComparison.OrdinalIgnoreCase))
            {
                TipoDocumento = Schema.Contains("CTe", StringComparison.OrdinalIgnoreCase) ? "Evento CT-e" : "Evento NF-e";

                var matchTpEvento = Regex.Match(XmlConteudo, @"<tpEvento>([0-9]+)</tpEvento>");
                if (matchTpEvento.Success) TipoEvento = matchTpEvento.Groups[1].Value;

                var matchDescEvento = Regex.Match(XmlConteudo, @"<descEvento>(.*?)</descEvento>");
                if (matchDescEvento.Success) NomeEvento = matchDescEvento.Groups[1].Value;
            }
        }

        public bool RequerCienciaAutomatica(string cnpjEmpresa)
        {
            // Apenas Resumos de NF-e sem ciência prévia
            // E que foram emitidos há menos de 10 dias!
            return Schema.Contains("resNFe") &&
                   !CienciaEnviada &&
                   (DateTimeOffset.Now - DataEmissao).TotalDays <= 10;
        }
        public void MarcarCienciaEnviada() => CienciaEnviada = true;
    }
}