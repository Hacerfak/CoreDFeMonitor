// src/CoreDFeMonitor.Application/Features/Documentos/Queries/ObterTotaisDocumentosQuery.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Queries
{
    public class ObterTotaisDocumentosQuery : IRequest<TotaisDocumentosDto> { }

    public class ObterTotaisDocumentosQueryHandler : IRequestHandler<ObterTotaisDocumentosQuery, TotaisDocumentosDto>
    {
        private readonly IDocumentoRepository _documentoRepository;

        public ObterTotaisDocumentosQueryHandler(IDocumentoRepository documentoRepository)
        {
            _documentoRepository = documentoRepository;
        }

        public async Task<TotaisDocumentosDto> Handle(ObterTotaisDocumentosQuery request, CancellationToken cancellationToken)
        {
            // O ideal seria que o repositório tivesse métodos de contagem específicos,
            // mas como é SQLite, podemos usar o repositório genérico por agora.
            // (Assumindo que o repositório possui ObterTodasAsync)
            var todos = await _documentoRepository.ObterTodasAsync(cancellationToken);

            var hoje = DateTime.UtcNow.Date;
            var primeiroDiaMes = new DateTime(hoje.Year, hoje.Month, 1);

            int totalHoje = todos.Count(d => d.DataProcessamento.Date == hoje);
            int totalMes = todos.Count(d => d.DataProcessamento.Date >= primeiroDiaMes);

            return new TotaisDocumentosDto(totalMes, totalHoje);
        }
    }
}