// src/CoreDFeMonitor.Application/Features/Documentos/Queries/ObterUltimosDocumentosQuery.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                .OrderByDescending(d => d.DataProcessamento)
                .Take(5)
                .Select(d => new DocumentoDto(
                    d.Id,
                    d.Nsu,
                    d.ChaveAcesso,
                    MapearSchema(d.Schema), // Função auxiliar
                    d.DataProcessamento,
                    d.CienciaEnviada))
                .ToList();

            return ultimos;
        }

        private string MapearSchema(string schema)
        {
            // Mapeamento simples para o utilizador
            if (schema.Contains("procNFe")) return "NFe Processada";
            if (schema.Contains("resNFe")) return "Resumo NFe";
            if (schema.Contains("resEvento")) return "Resumo Evento";
            if (schema.Contains("retEnvEvento")) return "Retorno Evento";
            return schema;
        }
    }
}