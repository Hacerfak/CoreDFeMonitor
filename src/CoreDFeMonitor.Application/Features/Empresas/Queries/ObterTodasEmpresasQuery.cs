using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Empresas.Queries
{
    public class ObterTodasEmpresasQuery : IRequest<List<Empresa>> { }

    public class ObterTodasEmpresasQueryHandler : IRequestHandler<ObterTodasEmpresasQuery, List<Empresa>>
    {
        private readonly IEmpresaRepository _empresaRepository;

        public ObterTodasEmpresasQueryHandler(IEmpresaRepository empresaRepository)
        {
            _empresaRepository = empresaRepository;
        }

        public async Task<List<Empresa>> Handle(ObterTodasEmpresasQuery request, CancellationToken cancellationToken)
        {
            var empresas = await _empresaRepository.ObterTodasAsync(cancellationToken);
            return empresas.ToList();
        }
    }
}