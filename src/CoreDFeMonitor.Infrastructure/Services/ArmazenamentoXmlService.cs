// src/CoreDFeMonitor.Infrastructure/Services/ArmazenamentoXmlService.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class ArmazenamentoXmlService : IArmazenamentoXmlService
    {
        private readonly ILogger<ArmazenamentoXmlService> _logger;

        public ArmazenamentoXmlService(ILogger<ArmazenamentoXmlService> logger)
        {
            _logger = logger;
        }

        public async Task SalvarXmlAsync(string cnpjEmpresa, string chaveAcesso, string schema, string xmlConteudo)
        {
            try
            {
                // Descobre quem é o emitente
                string cnpjEmitente = ExtrairCnpjEmitente(xmlConteudo);
                string tipoDiretorio = (cnpjEmitente == cnpjEmpresa) ? "Saidas" : "Entradas";

                string subPasta = "Outros";
                string nomeArquivo = $"{chaveAcesso}.xml";

                // LÓGICA RÍGIDA DE SEPARAÇÃO E NOMEAÇÃO
                if (schema.Contains("procNFe"))
                {
                    subPasta = "NFe_Completas";
                    nomeArquivo = $"NFE_COMPLETA_{chaveAcesso}.xml";
                }
                else if (schema.Contains("resNFe"))
                {
                    subPasta = "NFe_Resumos";
                    nomeArquivo = $"NFE_RESUMO_{chaveAcesso}.xml";
                }
                else if (schema.Contains("procCTe"))
                {
                    subPasta = "CTe_Completos";
                    nomeArquivo = $"CTE_COMPLETO_{chaveAcesso}.xml";
                }
                else if (schema.Contains("resCTe"))
                {
                    subPasta = "CTe_Resumos";
                    nomeArquivo = $"CTE_RESUMO_{chaveAcesso}.xml";
                }
                else if (schema.Contains("Evento", StringComparison.OrdinalIgnoreCase))
                {
                    subPasta = "Eventos";
                    string tpEvento = ExtrairTag(xmlConteudo, "tpEvento", "000000") ?? "000000";
                    string nSeqEvento = ExtrairTag(xmlConteudo, "nSeqEvento", "1") ?? "1";

                    // Adiciona o Tipo de Evento e Sequência no nome para evitar sobrescrita!
                    nomeArquivo = $"EVENTO_{tpEvento}_SEQ{nSeqEvento}_{chaveAcesso}.xml";
                }

                // Cria as pastas hierárquicas
                var pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var caminhoBase = Path.Combine(pastaDocumentos, "CoreDFeMonitor", "XMLs", cnpjEmpresa, tipoDiretorio, subPasta);

                if (!Directory.Exists(caminhoBase))
                {
                    Directory.CreateDirectory(caminhoBase);
                }

                string caminhoFinal = Path.Combine(caminhoBase, nomeArquivo);

                await File.WriteAllTextAsync(caminhoFinal, xmlConteudo);
                _logger.LogInformation("XML armazenado estruturalmente: {Caminho}", caminhoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar o XML da chave {Chave} no disco.", chaveAcesso);
            }
        }

        private string? ExtrairTag(string xml, string tag, string? padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string ExtrairCnpjEmitente(string xml)
        {
            var match = Regex.Match(xml, @"<emit>.*?<(CNPJ|CPF)>([0-9]+)</\1>.*?</emit>", RegexOptions.Singleline);
            return match.Success ? match.Groups[2].Value : "00000000000000";
        }
    }
}