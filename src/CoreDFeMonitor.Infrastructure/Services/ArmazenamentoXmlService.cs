// src/CoreDFeMonitor.Infrastructure/Services/ArmazenamentoXmlService.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Infrastructure.Services
{
    // Crie esta interface na pasta Core/Interfaces: 
    // public interface IArmazenamentoXmlService { Task SalvarXmlAsync(string cnpjEmpresa, string chaveAcesso, string schema, string xmlConteudo); }

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
                // 1. Descobrir quem é o emitente lendo o XML cru
                string cnpjEmitente = ExtrairCnpjEmitente(xmlConteudo);

                // 2. Definir se é Entrada ou Saída (Se o emitente sou eu, é Saída. Senão, é Entrada).
                string tipoDiretorio = (cnpjEmitente == cnpjEmpresa) ? "Saidas" : "Entradas";

                // 3. Montar a árvore de pastas: Meus Documentos > CoreDFeMonitor > XMLs > [CNPJ] > [Entrada/Saida]
                var pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var caminhoBase = Path.Combine(pastaDocumentos, "CoreDFeMonitor", "XMLs", cnpjEmpresa, tipoDiretorio);

                if (!Directory.Exists(caminhoBase))
                {
                    Directory.CreateDirectory(caminhoBase);
                }

                // 4. Nomear o arquivo de forma inteligente para não sobrescrever resumos com notas completas
                string prefixo = schema.Contains("res") ? "RESUMO_" : (schema.Contains("proc") ? "COMPLETO_" : "EVENTO_");
                string nomeArquivo = $"{prefixo}{chaveAcesso}.xml";
                string caminhoFinal = Path.Combine(caminhoBase, nomeArquivo);

                // 5. Salvar no disco
                await File.WriteAllTextAsync(caminhoFinal, xmlConteudo);

                _logger.LogInformation("XML salvo em disco: {Caminho}", caminhoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar o XML da chave {Chave} no disco.", chaveAcesso);
            }
        }

        private string ExtrairCnpjEmitente(string xml)
        {
            // Busca a tag <emit> e dentro dela o <CNPJ> ou <CPF>
            var match = Regex.Match(xml, @"<emit>.*?<(CNPJ|CPF)>([0-9]+)</\1>.*?</emit>", RegexOptions.Singleline);
            return match.Success ? match.Groups[2].Value : "00000000000000";
        }
    }
}