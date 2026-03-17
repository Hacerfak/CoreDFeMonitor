// src/CoreDFeMonitor.Infrastructure/Services/SefazService.cs
using System;
using System.IO;
using System.Net;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Utils;

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
    }
}