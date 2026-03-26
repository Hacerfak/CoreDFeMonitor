using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Empresas.Queries
{
    public class VerificarEmpresasCadastradasQuery : IRequest<bool> { }

    public class VerificarEmpresasCadastradasQueryHandler : IRequestHandler<VerificarEmpresasCadastradasQuery, bool>
    {
        private readonly IEmpresaRepository _empresaRepository;

        public VerificarEmpresasCadastradasQueryHandler(IEmpresaRepository empresaRepository)
        {
            _empresaRepository = empresaRepository;
        }

        public async Task<bool> Handle(VerificarEmpresasCadastradasQuery request, CancellationToken cancellationToken)
        {
            var empresas = await _empresaRepository.ObterTodasAsync(cancellationToken);
            return empresas.Any();
        }
    }
}