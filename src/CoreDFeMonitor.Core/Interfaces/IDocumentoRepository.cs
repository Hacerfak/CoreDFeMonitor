using CoreDFeMonitor.Core.Entities;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface IDocumentoRepository
    {
        Task AdicionarLoteAsync(IEnumerable<Documento> documentos, CancellationToken cancellationToken = default);
        Task<bool> ExisteNsuAsync(Guid empresaId, string nsu, CancellationToken cancellationToken = default);
        Task<List<Documento>> ObterTodasAsync(CancellationToken cancellationToken = default);
    }
}