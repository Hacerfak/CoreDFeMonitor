// src/CoreDFeMonitor.Core/Entities/Documento.cs
using System;

namespace CoreDFeMonitor.Core.Entities
{
    public class Documento
    {
        public Guid Id { get; private set; }
        public Guid EmpresaId { get; private set; }
        public string Nsu { get; private set; } = string.Empty;
        public string ChaveAcesso { get; private set; } = string.Empty;
        public string Schema { get; private set; } = string.Empty; // Identifica se é resNFe, procNFe, resEvento, etc.
        public string XmlConteudo { get; private set; } = string.Empty; // O XML real descomprimido
        public DateTime DataProcessamento { get; private set; }

        protected Documento() { }

        public Documento(Guid empresaId, string nsu, string schema, string xmlConteudo)
        {
            Id = Guid.NewGuid();
            EmpresaId = empresaId;
            Nsu = nsu.PadLeft(15, '0');
            Schema = schema;
            XmlConteudo = xmlConteudo;
            DataProcessamento = DateTime.UtcNow;

            // Uma extração simples da chave de acesso do XML (seja resumo ou proc)
            ChaveAcesso = ExtrairChave(xmlConteudo);
        }

        private string ExtrairChave(string xml)
        {
            // Busca a chave na tag chNFe (usada em 90% dos schemas da Sefaz)
            var match = System.Text.RegularExpressions.Regex.Match(xml, @"<chNFe>([0-9]{44})</chNFe>");
            return match.Success ? match.Groups[1].Value : "SEM_CHAVE";
        }
    }
}