// src/CoreDFeMonitor.Infrastructure/Services/SefazService.cs
using System.Net;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Utils;
using NFe.Servicos;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Servicos.ConsultaCadastro;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class SefazService : ISefazService
    {
        private readonly ICertificadoService _certificadoService;
        private readonly ILogger<SefazService> _logger; // <--- Logger

        public SefazService(ICertificadoService certificadoService, ILogger<SefazService> logger)
        {
            _certificadoService = certificadoService;
            _logger = logger;
        }

        internal ConfiguracaoServico CriarConfiguracaoZeus(Empresa empresa)
        {
            if (string.IsNullOrEmpty(empresa.CaminhoCertificado) || !File.Exists(empresa.CaminhoCertificado))
                throw new FileNotFoundException($"Arquivo do certificado não encontrado para o CNPJ {empresa.Cnpj}.");

            // 1. Conversão da UF (String do Core para Enum do Zeus)
            if (!Enum.TryParse(empresa.Uf, out Estado estadoSefaz))
                throw new ArgumentException($"UF {empresa.Uf} inválida para a Sefaz.");

            // 2. Nova Configuração do Certificado exigida pelo Zeus Fiscal
            var configCertificado = new ConfiguracaoCertificado()
            {
                TipoCertificado = TipoCertificado.A1ByteArray,
                ArrayBytesArquivo = File.ReadAllBytes(empresa.CaminhoCertificado),
                Senha = empresa.SenhaCertificado ?? string.Empty,
                ManterDadosEmCache = false,
                SignatureMethodSignedXml = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                DigestMethodReference = "http://www.w3.org/2000/09/xmldsig#sha1"
            };

            // 3. Configuração do Serviço Baseada no DemoNFe
            var config = new ConfiguracaoServico
            {
                ValidarCertificadoDoServidor = false,
                SalvarXmlServicos = false, // Evita poluir o disco com arquivos de log desnecessários
                ValidarSchemas = false, // Em um cenário de monitor, validaremos a estrutura do schema sob demanda se necessário
                ProtocoloDeSeguranca = SecurityProtocolType.Tls13,
                RemoverAcentos = true,
                DefineVersaoServicosAutomaticamente = true, // Zeus tentará resolver a versão WSDL dinamicamente
                VersaoLayout = VersaoServico.Versao400,
                ModeloDocumento = ModeloDocumento.NFe,
                tpEmis = NFe.Classes.Informacoes.Identificacao.Tipos.TipoEmissao.teNormal,
                tpAmb = TipoAmbiente.Producao, // Mudaremos para Homologacao se precisar testar futuramente
                cUF = estadoSefaz, // INFORMA O ESTADO (Crucial para o DNS correto do WSDL)
                TimeOut = 30000,
                Certificado = configCertificado // Passando o objeto de certificado estruturado
            };

            return config;
        }

        public bool ValidarConfiguracao(Empresa empresa)
        {
            try
            {
                // Verifica se a configuração é montada com sucesso
                var config = CriarConfiguracaoZeus(empresa);

                // Usamos o nosso CertificadoService (X509CertificateLoader) apenas para 
                // garantir preventivamente que o arquivo PFX é de fato um certificado íntegro e ler sua validade, 
                // antes mesmo de mandar requisições à Sefaz.
                var certValidador = _certificadoService.ObterCertificadoDoArquivo(empresa.CaminhoCertificado!, empresa.SenhaCertificado ?? string.Empty);

                return certValidador.HasPrivateKey && certValidador.NotAfter > DateTime.Now;
            }
            catch
            {
                return false;
            }
        }

        public async Task<SefazCadastroResult> ConsultarCadastroAsync(string uf, string caminhoCertificado, string senha)
        {
            string cnpjBase = string.Empty;
            string razaoBase = string.Empty;

            try
            {
                _logger.LogInformation("--- INICIANDO CONSULTA SEFAZ ---");

                var cert = _certificadoService.ObterCertificadoDoArquivo(caminhoCertificado, senha);
                var subject = cert.Subject;
                _logger.LogInformation("Certificado carregado. Subject bruto: {Subject}", subject);

                // Extração melhorada (Lida com vírgulas e formatações diferentes)
                var cnpjMatch = Regex.Match(subject, @"([0-9]{14})");
                cnpjBase = cnpjMatch.Success ? cnpjMatch.Groups[1].Value : "";
                _logger.LogInformation("CNPJ extraído do certificado: '{Cnpj}'", cnpjBase);

                var razaoMatch = Regex.Match(subject, @"CN=([^:,]+)");
                razaoBase = razaoMatch.Success ? razaoMatch.Groups[1].Value.Trim() : "Razão Social Não Identificada";
                _logger.LogInformation("Razão Social extraída do certificado: '{Razao}'", razaoBase);

                if (string.IsNullOrEmpty(cnpjBase))
                {
                    _logger.LogWarning("FALHA: Não foi possível encontrar 14 números no Subject do Certificado.");
                    return new SefazCadastroResult(false, "", "", null, null, null, null, null, null, null, null, "Não foi possível extrair o CNPJ do Certificado.");
                }

                var configCertificado = new DFe.Utils.ConfiguracaoCertificado()
                {
                    TipoCertificado = DFe.Utils.TipoCertificado.A1ByteArray,
                    ArrayBytesArquivo = File.ReadAllBytes(caminhoCertificado),
                    Senha = senha,
                    SignatureMethodSignedXml = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                    DigestMethodReference = "http://www.w3.org/2000/09/xmldsig#sha1"
                };

                if (!Enum.TryParse(uf.ToUpper(), out Estado estadoSefaz))
                    return new SefazCadastroResult(false, cnpjBase, razaoBase, null, null, null, null, null, null, null, null, "UF inválida.");

                var configTemp = new NFe.Utils.ConfiguracaoServico
                {
                    tpAmb = DFe.Classes.Flags.TipoAmbiente.Producao,
                    tpEmis = TipoEmissao.teNormal,
                    cUF = estadoSefaz,
                    ModeloDocumento = DFe.Classes.Flags.ModeloDocumento.NFe,
                    Certificado = configCertificado,
                    TimeOut = 20000,
                    ValidarSchemas = false,
                    DefineVersaoServicosAutomaticamente = false,
                    VersaoNfeConsultaCadastro = DFe.Classes.Flags.VersaoServico.Versao400,
                    VersaoLayout = DFe.Classes.Flags.VersaoServico.Versao400
                };

                using var servicoNfe = new ServicosNFe(configTemp);

                _logger.LogInformation("Enviando requisição WSDL para a SEFAZ de {UF}...", estadoSefaz);
                var retornoSefaz = servicoNfe.NfeConsultaCadastro(uf.ToUpper(), ConsultaCadastroTipoDocumento.Cnpj, cnpjBase);

                _logger.LogInformation("Resposta da SEFAZ recebida. cStat: {cStat} | xMotivo: {xMotivo}",
                    retornoSefaz?.Retorno?.infCons?.cStat,
                    retornoSefaz?.Retorno?.infCons?.xMotivo);

                if (retornoSefaz?.Retorno?.infCons?.infCad != null)
                {
                    _logger.LogInformation("Dados de endereço (infCad) retornados pela Sefaz com sucesso!");
                    var dados = retornoSefaz.Retorno.infCons.infCad;
                    var ender = dados.ender;

                    long? codigoIbge = null;
                    if (ender != null && !string.IsNullOrWhiteSpace(ender.cMun) && long.TryParse(ender.cMun, out var ibgeParsed))
                        codigoIbge = ibgeParsed;

                    return new SefazCadastroResult(
                        true, cnpjBase, dados.xNome ?? razaoBase, dados.IE, ender?.xLgr, ender?.nro,
                        ender?.xCpl, ender?.xBairro, codigoIbge, ender?.xMun, ender?.CEP, string.Empty
                    );
                }

                _logger.LogWarning("Sefaz respondeu positivamente, mas sem dados de cadastro (infCad vazio).");
                return new SefazCadastroResult(
                    true, cnpjBase, razaoBase, null, null, null, null, null, null, null, null,
                    "Sucesso parcial: A Sefaz não retornou os dados de endereço."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXCEÇÃO CAPTURADA: {Message}", ex.Message);

                if (string.IsNullOrEmpty(cnpjBase))
                    return new SefazCadastroResult(false, "", "", null, null, null, null, null, null, null, null, $"Erro ao ler o certificado (verifique a senha): {ex.Message}");

                return new SefazCadastroResult(
                    true, cnpjBase, razaoBase, null, null, null, null, null, null, null, null,
                    $"AVISO SEFAZ: {ex.Message}. (Avançando apenas com os dados do Certificado)" // <-- Isto vai aparecer na UI
                );
            }
        }
    }
}