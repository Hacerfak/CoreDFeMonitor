// Caminho: src/CoreDFeMonitor.Application/Features/Emitentes/Queries/ObterEmitentesQuery.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Emitentes.Queries
{
    public class ObterEmitentesQuery : IRequest<List<Emitente>> { }

    public class ObterEmitentesQueryHandler : IRequestHandler<ObterEmitentesQuery, List<Emitente>>
    {
        private readonly IEmitenteRepository _emitenteRepository;

        public ObterEmitentesQueryHandler(IEmitenteRepository emitenteRepository)
        {
            _emitenteRepository = emitenteRepository;
        }

        public async Task<List<Emitente>> Handle(ObterEmitentesQuery request, CancellationToken cancellationToken)
        {
            // Usando o repositório da forma correta!
            return await _emitenteRepository.ObterTodosAsync(cancellationToken);
        }
    }
}