using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Commands
{
    public class ManifestarDocumentoCommand : IRequest<(bool Sucesso, string Mensagem)>
    {
        public Guid DocumentoId { get; set; }
        public int CodigoManifestacao { get; set; }
        public string Justificativa { get; set; } = string.Empty;
    }

    public class ManifestarDocumentoCommandHandler : IRequestHandler<ManifestarDocumentoCommand, (bool Sucesso, string Mensagem)>
    {
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ISefazService _sefazService;

        public ManifestarDocumentoCommandHandler(IDocumentoRepository documentoRepository, IEmpresaRepository empresaRepository, ISefazService sefazService)
        {
            _documentoRepository = documentoRepository;
            _empresaRepository = empresaRepository;
            _sefazService = sefazService;
        }

        public async Task<(bool Sucesso, string Mensagem)> Handle(ManifestarDocumentoCommand request, CancellationToken cancellationToken)
        {
            var docs = await _documentoRepository.ObterTodasAsync(cancellationToken);
            var documento = docs.FirstOrDefault(d => d.Id == request.DocumentoId);
            if (documento == null) return (false, "Documento não encontrado.");

            var empresa = await _empresaRepository.ObterPorIdAsync(documento.EmpresaId, cancellationToken);
            var resultadoSefaz = await _sefazService.EnviarManifestacaoAsync(empresa, documento.ChaveAcesso, request.CodigoManifestacao, request.Justificativa);

            if (resultadoSefaz.Sucesso)
            {
                documento.AtualizarManifestacao(request.CodigoManifestacao);
                await _documentoRepository.AtualizarAsync(documento, cancellationToken);
            }
            return (resultadoSefaz.Sucesso, resultadoSefaz.Mensagem);
        }
    }
}