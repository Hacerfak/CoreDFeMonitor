using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Services;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Commands
{
    public class BaixarXmlPorChaveCommand : IRequest<(bool Sucesso, string Mensagem)>
    {
        public Guid EmpresaId { get; set; }
        public string ChaveAcesso { get; set; } = string.Empty;
    }

    public class BaixarXmlPorChaveCommandHandler : IRequestHandler<BaixarXmlPorChaveCommand, (bool Sucesso, string Mensagem)>
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IEmitenteRepository _emitenteRepository;
        private readonly ISefazService _sefazService;
        private readonly IArmazenamentoXmlService _armazenamentoXmlService;

        public BaixarXmlPorChaveCommandHandler(
            IEmpresaRepository empresaRepository,
            IDocumentoRepository documentoRepository,
            IEmitenteRepository emitenteRepository,
            ISefazService sefazService,
            IArmazenamentoXmlService armazenamentoXmlService)
        {
            _empresaRepository = empresaRepository;
            _documentoRepository = documentoRepository;
            _emitenteRepository = emitenteRepository;
            _sefazService = sefazService;
            _armazenamentoXmlService = armazenamentoXmlService;
        }

        public async Task<(bool Sucesso, string Mensagem)> Handle(BaixarXmlPorChaveCommand request, CancellationToken cancellationToken)
        {
            string chaveLimpa = request.ChaveAcesso?.Trim() ?? string.Empty;
            if (chaveLimpa.Length != 44)
                return (false, "A Chave de Acesso deve conter exatamente 44 dígitos.");

            var empresa = await _empresaRepository.ObterPorIdAsync(request.EmpresaId, cancellationToken);
            if (empresa == null) return (false, "Empresa destinatária não encontrada.");

            var resultado = await _sefazService.BaixarDocumentoPorChaveAsync(empresa, chaveLimpa);
            if (!resultado.Sucesso || !resultado.Documentos.Any())
                return (false, string.IsNullOrEmpty(resultado.Mensagem) ? "Nenhum XML retornado para a chave informada." : resultado.Mensagem);

            int salvos = 0;
            foreach (var docZip in resultado.Documentos)
            {
                bool jaExiste = await _documentoRepository.ExisteNsuAsync(empresa.Id, docZip.Nsu, cancellationToken);
                if (!jaExiste)
                {
                    string xml = docZip.XmlDescompactado;
                    string schemaLower = docZip.Schema.ToLower();

                    string cnpjEmit = chaveLimpa.Substring(6, 14);
                    var emitente = await _emitenteRepository.ObterPorCnpjAsync(cnpjEmit, cancellationToken);
                    if (emitente == null)
                    {
                        emitente = new Emitente { Cnpj = cnpjEmit, RazaoSocial = "EMITENTE CONSULTADO VIA CHAVE" };
                        await _emitenteRepository.AdicionarAsync(emitente, cancellationToken);
                    }

                    string tipoDoc = schemaLower.Contains("resnfe") ? "Resumo" : (schemaLower.Contains("evento") ? "Evento" : "NFe");

                    var novoDoc = new Documento(empresa.Id, docZip.Nsu, docZip.Schema, xml)
                    {
                        EmitenteId = emitente.Id,
                        ChaveAcesso = chaveLimpa,
                        TipoDocumento = tipoDoc
                    };

                    await _documentoRepository.AdicionarLoteAsync(new[] { novoDoc }, cancellationToken);
                    _ = _armazenamentoXmlService.SalvarXmlAsync(empresa.Cnpj, chaveLimpa, docZip.Schema, xml);
                    salvos++;
                }
            }

            return (true, salvos > 0 ? "XML baixado e armazenado na base com sucesso!" : "O documento desta chave já existia no banco.");
        }
    }
}