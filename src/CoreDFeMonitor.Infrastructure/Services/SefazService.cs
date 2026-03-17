// src/CoreDFeMonitor.Infrastructure/Services/SefazService.cs
using System.Net;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Utils;
using NFe.Servicos;
using System.Text.RegularExpressions;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class SefazService : ISefazService
    {
        private readonly ICertificadoService _certificadoService;

        public SefazService(ICertificadoService certificadoService)
        {
            _certificadoService = certificadoService;
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

        // src/CoreDFeMonitor.Infrastructure/Services/SefazService.cs

        public async Task<SefazCadastroResult> ConsultarCadastroAsync(string uf, string caminhoCertificado, string senha)
        {
            // 1. Declaramos as variáveis FORA do try para o catch as poder usar
            string cnpjBase = string.Empty;
            string razaoBase = string.Empty;

            try
            {
                // 2. Extração dos dados do certificado
                var cert = _certificadoService.ObterCertificadoDoArquivo(caminhoCertificado, senha);
                var subject = cert.Subject;

                var cnpjMatch = Regex.Match(subject, @"([0-9]{14})");
                cnpjBase = cnpjMatch.Success ? cnpjMatch.Groups[1].Value : "";

                var razaoMatch = Regex.Match(subject, @"CN=([^:]+)");
                razaoBase = razaoMatch.Success ? razaoMatch.Groups[1].Value.Trim() : "Razão Social Não Identificada";

                if (string.IsNullOrEmpty(cnpjBase))
                    return new SefazCadastroResult(false, "", "", null, null, null, null, null, null, null, null, "Não foi possível extrair o CNPJ do Certificado.");

                var configCertificado = new DFe.Utils.ConfiguracaoCertificado()
                {
                    TipoCertificado = DFe.Utils.TipoCertificado.A1ByteArray,
                    ArrayBytesArquivo = File.ReadAllBytes(caminhoCertificado),
                    Senha = senha,
                    SignatureMethodSignedXml = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
                    DigestMethodReference = "http://www.w3.org/2000/09/xmldsig#sha1"
                };

                if (!Enum.TryParse(uf.ToUpper(), out DFe.Classes.Entidades.Estado estadoSefaz))
                    return new SefazCadastroResult(false, cnpjBase, razaoBase, null, null, null, null, null, null, null, null, "UF inválida.");

                var configTemp = new NFe.Utils.ConfiguracaoServico
                {
                    tpAmb = DFe.Classes.Flags.TipoAmbiente.Producao,
                    cUF = estadoSefaz,
                    ModeloDocumento = DFe.Classes.Flags.ModeloDocumento.NFe,
                    Certificado = configCertificado,
                    TimeOut = 20000,
                    ValidarSchemas = false, // Desligado para evitar erro da v4.00 na Sefaz
                    DefineVersaoServicosAutomaticamente = false,
                    VersaoNfeConsultaCadastro = DFe.Classes.Flags.VersaoServico.Versao400,
                    VersaoLayout = DFe.Classes.Flags.VersaoServico.Versao400
                };

                using var servicoNfe = new ServicosNFe(configTemp);
                var retornoSefaz = servicoNfe.NfeConsultaCadastro(uf.ToUpper(), NFe.Classes.Servicos.ConsultaCadastro.ConsultaCadastroTipoDocumento.Cnpj, cnpjBase);

                if (retornoSefaz?.Retorno?.infCons?.infCad != null)
                {
                    var dados = retornoSefaz.Retorno.infCons.infCad;
                    var ender = dados.ender;

                    long? codigoIbge = null;
                    if (ender != null && !string.IsNullOrWhiteSpace(ender.cMun) && long.TryParse(ender.cMun, out var ibgeParsed))
                    {
                        codigoIbge = ibgeParsed;
                    }

                    return new SefazCadastroResult(
                        true,
                        cnpjBase,
                        dados.xNome ?? razaoBase,
                        dados.IE,
                        ender?.xLgr,
                        ender?.nro,
                        ender?.xCpl,
                        ender?.xBairro,
                        codigoIbge,
                        ender?.xMun,
                        ender?.CEP,
                        string.Empty
                    );
                }

                return new SefazCadastroResult(
                    true, cnpjBase, razaoBase, null, null, null, null, null, null, null, null,
                    "Sucesso parcial: A Sefaz consultada não retornou os dados completos de endereço."
                );
            }
            catch (Exception ex)
            {
                // Se o CNPJ for vazio aqui, significa que o erro ocorreu ANTES de consultar a Sefaz 
                // (ex: senha incorreta ou arquivo PFX corrompido). Portanto, bloqueamos.
                if (string.IsNullOrEmpty(cnpjBase))
                {
                    return new SefazCadastroResult(false, "", "", null, null, null, null, null, null, null, null, $"Erro ao ler o certificado (verifique a senha): {ex.Message}");
                }

                // Se o CNPJ foi preenchido, o certificado é válido e o erro foi de conexão com a Sefaz. Avançamos!
                return new SefazCadastroResult(
                    true,
                    cnpjBase,
                    razaoBase,
                    null, null, null, null, null, null, null, null,
                    $"Aviso: A SEFAZ ({uf}) está indisponível ou rejeitou a consulta ({ex.Message}). Pode prosseguir preenchendo os dados manualmente."
                );
            }
        }
    }
}