// src/CoreDFeMonitor.Core/Entities/Documento.cs
using System;
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
        public DateTime DataProcessamento { get; private set; }

        // NOVAS COLUNAS PARA CLASSIFICAÇÃO EXATA
        public string TipoDocumento { get; private set; } = "Desconhecido";
        public string TipoEvento { get; private set; } = string.Empty;
        public string NomeEvento { get; private set; } = string.Empty;

        protected Documento() { } // Para o EF Core

        public Documento(Guid empresaId, string nsu, string schema, string xmlConteudo)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId;
            Nsu = nsu;
            Schema = schema;
            XmlConteudo = xmlConteudo;
            DataProcessamento = DateTime.UtcNow;
            CienciaEnviada = false;

            ProcessarMetadadosDoXml();
        }

        private void ProcessarMetadadosDoXml()
        {
            // Extrai a Chave de Acesso
            var matchChave = Regex.Match(XmlConteudo, @"<ch(?:NFe|CTe)>([0-9]{44})</ch(?:NFe|CTe)>");
            ChaveAcesso = matchChave.Success ? matchChave.Groups[1].Value : "SEM_CHAVE_NO_RESUMO";

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
            // Apenas Resumos de NF-e sem ciência prévia precisam da manifestação para baixar o XML completo
            return Schema.Contains("resNFe") && !CienciaEnviada;
        }

        public void MarcarCienciaEnviada() => CienciaEnviada = true;
    }
}