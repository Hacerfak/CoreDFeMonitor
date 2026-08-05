using System;
using System.Diagnostics;
using System.IO;
using CoreDFeMonitor.Core.Interfaces;
using DFe.Utils;
using FastReport;
using FastReport.Export.PdfSimple;
using NFe.Classes;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class ImpressaoService : IImpressaoService
    {
        public void VisualizarDanfe(string xmlConteudo, string chaveAcesso)
        {
            try
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "NFeRetrato.frx");

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException("Layout do DANFE não encontrado.");

                var proc = FuncoesXml.XmlStringParaClasse<nfeProc>(xmlConteudo);

                using var report = new Report();
                report.Load(templatePath);

                // Registra a fonte de dados usando o mesmo padrão do MDF-e[cite: 4]
                report.RegisterData(new[] { proc }, "NFe", 20);
                report.GetDataSource("NFe").Enabled = true;

                // Injeta os parâmetros obrigatórios do relatório da NF-e
                report.SetParameterValue("ImprimirUnidQtdeValor", 0); // 0 = Comercial
                report.SetParameterValue("ImprimirDescPorc", false);
                report.SetParameterValue("ImprimirTotalLiquido", true);
                report.SetParameterValue("DuasLinhas", false);
                report.SetParameterValue("ExibeRetencoes", false);
                report.SetParameterValue("ImprimirISSQN", false);
                report.SetParameterValue("ExibeCampoFatura", true);
                report.SetParameterValue("ExibirTotalTributos", false);
                report.SetParameterValue("DecimaisValorUnitario", 4);
                report.SetParameterValue("DecimaisQuantidadeItem", 4);
                report.SetParameterValue("DataHoraImpressao", DateTime.Now);
                report.SetParameterValue("Desenvolvedor", "Eder Gross Cichelero");
                report.SetParameterValue("QuebrarLinhasObservacao", true);

                report.Prepare();

                string caminhoPdf = Path.Combine(Path.GetTempPath(), $"DANFE_{chaveAcesso}.pdf");

                using (var pdfExport = new PDFSimpleExport())
                {
                    report.Export(pdfExport, caminhoPdf);
                }

                Process.Start(new ProcessStartInfo(caminhoPdf) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao gerar DANFE: {ex.Message}");
            }
        }
    }
}