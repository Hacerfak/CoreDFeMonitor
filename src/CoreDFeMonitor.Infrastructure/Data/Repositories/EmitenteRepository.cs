using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data.Repositories
{
    public class EmitenteRepository : IEmitenteRepository
    {
        private readonly DFeMonitorDbContext _context;

        public EmitenteRepository(DFeMonitorDbContext context)
        {
            _context = context;
        }

        public async Task<Emitente?> ObterPorCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        {
            return await _context.Emitentes.FirstOrDefaultAsync(e => e.Cnpj == cnpj, cancellationToken);
        }

        public async Task AdicionarAsync(Emitente emitente, CancellationToken cancellationToken = default)
        {
            await _context.Emitentes.AddAsync(emitente, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Emitente>> ObterTodosAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Emitentes
                .AsNoTracking()
                .OrderBy(e => e.RazaoSocial)
                .ToListAsync(cancellationToken);
        }
    }
}