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

        public async Task<List<Documento>> ObterTodasAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documentos
                .Include(d => d.Emitente)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task AtualizarAsync(Documento documento, CancellationToken cancellationToken)
        {
            _context.Documentos.Update(documento);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<Documento>> ObterPendentesDeCienciaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            var dataLimite = DateTimeOffset.Now.AddDays(-10);

            // 1. Vai no SQLite e traz APENAS as notas pendentes (Isso o banco entende bem)
            var pendentesDoBanco = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId &&
                            d.Schema.Contains("resNFe") &&
                            !d.CienciaEnviada)
                .ToListAsync(cancellationToken);

            // 2. Filtra na memória do C# as datas, a chave e IGNORA canceladas/denegadas
            return pendentesDoBanco
                .Where(d => d.DataEmissao >= dataLimite &&
                            !d.ChaveAcesso.StartsWith("SEM_CHAVE") &&
                            !d.XmlConteudo.Contains("<cSitNFe>2</cSitNFe>") && // Ignora Denegadas
                            !d.XmlConteudo.Contains("<cSitNFe>3</cSitNFe>"))   // Ignora Canceladas
                .Take(20)
                .ToList();
        }
    }
}