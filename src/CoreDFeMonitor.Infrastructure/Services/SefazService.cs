// src/CoreDFeMonitor.Infrastructure/Services/SefazService.cs
using System.Net;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Utils;
using NFe.Servicos;
using CTe.Servicos.DistribuicaoDFe;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Servicos.ConsultaCadastro;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class SefazService : ISefazService
    {
        private readonly ICertificadoService _certificadoService;
        private readonly ILogger<SefazService> _logger;

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
                tpEmis = TipoEmissao.teNormal,
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

                var configTemp = new ConfiguracaoServico
                {
                    tpAmb = TipoAmbiente.Producao,
                    tpEmis = TipoEmissao.teNormal,
                    cUF = estadoSefaz,
                    ModeloDocumento = ModeloDocumento.NFe,
                    Certificado = configCertificado,
                    TimeOut = 20000,
                    ValidarSchemas = false,
                    DefineVersaoServicosAutomaticamente = true,
                    VersaoLayout = VersaoServico.Versao400
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

        // ==========================================================
        // NF-E / EVENTOS
        // ==========================================================
        public async Task<SefazDistribuicaoResult> BaixarDocumentosAsync(Empresa empresa)
        {
            var documentosLidos = new List<DocumentoZip>();
            string nsuAtual = string.IsNullOrEmpty(empresa.UltimoNsu) ? "000000000000000" : empresa.UltimoNsu.PadLeft(15, '0');

            try
            {
                var config = CriarConfiguracaoZeus(empresa);
                using var servicoNfe = new ServicosNFe(config);

                string ufArg = ((int)config.cUF).ToString();
                var retorno = servicoNfe.NfeDistDFeInteresse(ufArg, empresa.Cnpj, nsuAtual);

                if (retorno?.Retorno == null)
                    return new SefazDistribuicaoResult(false, nsuAtual, "Sefaz NF-e não respondeu.", documentosLidos);

                var ret = retorno.Retorno;

                if (ret.cStat == 138 && ret.loteDistDFeInt != null)
                {
                    foreach (var docZip in ret.loteDistDFeInt)
                    {
                        var xmlDescompactado = Compressao.Unzip(docZip.XmlNfe);
                        documentosLidos.Add(new DocumentoZip(docZip.NSU.ToString(), docZip.schema, xmlDescompactado));
                    }
                    _logger.LogInformation("Baixados {Count} novos documentos da Sefaz (NF-e) para {CNPJ}.", documentosLidos.Count, empresa.Cnpj);
                }

                string nsuParaGravar = string.IsNullOrEmpty(ret.ultNSU.ToString()) ? nsuAtual : ret.ultNSU.ToString();
                return new SefazDistribuicaoResult(true, nsuParaGravar, ret.xMotivo ?? "", documentosLidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha de comunicação na Sefaz (NF-e): {Message}", ex.Message);
                return new SefazDistribuicaoResult(false, nsuAtual, $"Falha Sefaz NF-e: {ex.Message}", documentosLidos);
            }
        }

        public async Task<SefazManifestacaoResult> EnviarCienciaOperacaoAsync(Empresa empresa, string chaveAcesso)
        {
            try
            {
                var config = CriarConfiguracaoZeus(empresa);
                using var servicoNfe = new ServicosNFe(config);

                int idLote = 1;
                int seqEvento = 1;

                _logger.LogInformation("Disparando Evento de Ciência (210210) para a Chave {Chave}", chaveAcesso);

                var retorno = servicoNfe.RecepcaoEventoManifestacaoDestinatario(
                    idLote, seqEvento, chaveAcesso,
                    NFe.Classes.Servicos.Tipos.NFeTipoEvento.TeMdCienciaDaOperacao,
                    empresa.Cnpj);

                if (retorno?.Retorno?.retEvento != null && retorno.Retorno.retEvento.Count > 0)
                {
                    var retEv = retorno.Retorno.retEvento[0].infEvento;
                    if (retEv.cStat == 135 || retEv.cStat == 573)
                        return new SefazManifestacaoResult(true, $"[{retEv.cStat}] {retEv.xMotivo}");

                    return new SefazManifestacaoResult(false, $"Rejeição Sefaz [{retEv.cStat}]: {retEv.xMotivo}");
                }

                return new SefazManifestacaoResult(false, "Sefaz retornou vazio para o Evento.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao emitir Ciência: {Message}", ex.Message);
                return new SefazManifestacaoResult(false, $"Erro Local: {ex.Message}");
            }
        }

        // ==========================================================
        // CT-E
        // ==========================================================
        public async Task<SefazDistribuicaoResult> BaixarDocumentosCteAsync(Empresa empresa)
        {
            var documentosLidos = new List<DocumentoZip>();
            string nsuAtual = string.IsNullOrEmpty(empresa.UltimoNsuCte) ? "000000000000000" : empresa.UltimoNsuCte.PadLeft(15, '0');

            try
            {
                if (!Enum.TryParse(empresa.Uf.ToUpper(), out Estado estadoSefaz))
                    return new SefazDistribuicaoResult(false, nsuAtual, "UF inválida.", documentosLidos);

                var configCertificado = new ConfiguracaoCertificado()
                {
                    TipoCertificado = TipoCertificado.A1ByteArray,
                    ArrayBytesArquivo = File.ReadAllBytes(empresa.CaminhoCertificado!),
                    Senha = empresa.SenhaCertificado ?? string.Empty,
                    SignatureMethodSignedXml = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                    DigestMethodReference = "http://www.w3.org/2000/09/xmldsig#sha1"
                };

                var configCte = new CTe.Classes.ConfiguracaoServico
                {
                    tpAmb = TipoAmbiente.Producao,
                    cUF = estadoSefaz,
                    ConfiguracaoCertificado = configCertificado,
                    TimeOut = 30000,

                    // CORREÇÃO 3: Desliga a validação E aponta o diretório para evitar a Exception
                    IsValidaSchemas = false,
                    DiretorioSchemas = AppDomain.CurrentDomain.BaseDirectory
                };

                var servicoCte = new ServicoCTeDistribuicaoDFe(configCte);
                string ufArg = ((int)estadoSefaz).ToString();

                var retorno = servicoCte.CTeDistDFeInteresse(ufArg, empresa.Cnpj, nsuAtual);

                if (retorno?.Retorno == null)
                    return new SefazDistribuicaoResult(false, nsuAtual, "Sefaz CT-e não respondeu.", documentosLidos);

                var ret = retorno.Retorno;

                if (ret.cStat == 138 && ret.loteDistDFeInt != null)
                {
                    foreach (var docZip in ret.loteDistDFeInt)
                    {
                        var xmlDescompactado = Compressao.Unzip(docZip.XmlNfe);
                        documentosLidos.Add(new DocumentoZip(docZip.NSU.ToString(), docZip.schema, xmlDescompactado));
                    }
                    _logger.LogInformation("Baixados {Count} novos CT-es da Sefaz para {CNPJ}.", documentosLidos.Count, empresa.Cnpj);
                }

                string nsuParaGravar = string.IsNullOrEmpty(ret.ultNSU.ToString()) ? nsuAtual : ret.ultNSU.ToString();
                return new SefazDistribuicaoResult(true, nsuParaGravar, ret.xMotivo ?? "", documentosLidos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na distribuição de CT-e: {Message}", ex.Message);
                return new SefazDistribuicaoResult(false, nsuAtual, $"Falha Sefaz CT-e: {ex.Message}", documentosLidos);
            }
        }
    }
}