using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Commands
{
    public class ConsultarStatusDocumentoCommand : IRequest<(bool Sucesso, string Mensagem)>
    {
        public Guid DocumentoId { get; set; }
    }

    public class ConsultarStatusDocumentoCommandHandler : IRequestHandler<ConsultarStatusDocumentoCommand, (bool Sucesso, string Mensagem)>
    {
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ISefazService _sefazService;

        public ConsultarStatusDocumentoCommandHandler(IDocumentoRepository documentoRepository, IEmpresaRepository empresaRepository, ISefazService sefazService)
        {
            _documentoRepository = documentoRepository;
            _empresaRepository = empresaRepository;
            _sefazService = sefazService;
        }

        public async Task<(bool Sucesso, string Mensagem)> Handle(ConsultarStatusDocumentoCommand request, CancellationToken cancellationToken)
        {
            var docs = await _documentoRepository.ObterTodasAsync(cancellationToken);
            var doc = docs.FirstOrDefault(d => d.Id == request.DocumentoId);

            if (doc == null)
                return (false, "Documento não encontrado.");

            var empresa = await _empresaRepository.ObterPorIdAsync(doc.EmpresaId, cancellationToken);
            if (empresa == null)
                return (false, "Empresa não encontrada.");

            return await _sefazService.ConsultarStatusNFeAsync(empresa, doc.ChaveAcesso);
        }
    }
}