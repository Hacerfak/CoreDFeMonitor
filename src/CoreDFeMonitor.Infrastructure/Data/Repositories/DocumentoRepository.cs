using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data.Repositories
{
    public class DocumentoRepository : IDocumentoRepository
    {
        private readonly DFeMonitorDbContext _context;

        public DocumentoRepository(DFeMonitorDbContext context) => _context = context;

        public async Task AdicionarLoteAsync(IEnumerable<Documento> documentos, CancellationToken cancellationToken = default)
        {
            await _context.Documentos.AddRangeAsync(documentos, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExisteNsuAsync(Guid empresaId, string nsu, CancellationToken cancellationToken = default)
        {
            return await _context.Documentos.AnyAsync(d => d.EmpresaId == empresaId && d.Nsu == nsu, cancellationToken);
        }
    }
}