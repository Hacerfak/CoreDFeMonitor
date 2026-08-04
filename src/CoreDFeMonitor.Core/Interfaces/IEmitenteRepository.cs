using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface IEmitenteRepository
    {
        Task<Emitente?> ObterPorCnpjAsync(string cnpj, CancellationToken cancellationToken = default);
        Task AdicionarAsync(Emitente emitente, CancellationToken cancellationToken = default);
        Task<List<Emitente>> ObterTodosAsync(CancellationToken cancellationToken = default);
    }
}