using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Queries
{
    public class ObterUltimosDocumentosQuery : IRequest<List<DocumentoDto>> { }

    public class ObterUltimosDocumentosQueryHandler : IRequestHandler<ObterUltimosDocumentosQuery, List<DocumentoDto>>
    {
        private readonly IDocumentoRepository _documentoRepository;

        public ObterUltimosDocumentosQueryHandler(IDocumentoRepository documentoRepository)
        {
            _documentoRepository = documentoRepository;
        }

        public async Task<List<DocumentoDto>> Handle(ObterUltimosDocumentosQuery request, CancellationToken cancellationToken)
        {
            var documentos = await _documentoRepository.ObterTodasAsync(cancellationToken);

            // Pega os 5 mais recentes baixados
            var ultimos = documentos
                .OrderByDescending(d => d.DataEmissao)
                .Take(5)
                .Select(d =>
                {
                    // Lógica para deixar a Grid do Dashboard bonita
                    string schemaDisplay = d.TipoDocumento;

                    // Se for evento e tivermos o nome exato (ex: "Carta de Correção"), usamos o nome!
                    if (d.TipoDocumento.StartsWith("Evento") && !string.IsNullOrEmpty(d.NomeEvento))
                    {
                        schemaDisplay = d.NomeEvento;
                    }

                    return new DocumentoDto(
                        d.Id,
                        d.Nsu,
                        d.ChaveAcesso,
                        schemaDisplay,
                        d.DataEmissao,
                        d.CienciaEnviada);
                })
                .ToList();

            return ultimos;
        }
    }
}