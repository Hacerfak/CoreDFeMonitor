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

        public async Task AtualizarAsync(Documento documento, CancellationToken cancellationToken = default)
        {
            // 1. Verifica se o documento já está rastreado e o desanexa
            var entidadeRastreada = _context.Documentos.Local.FirstOrDefault(d => d.Id == documento.Id);
            if (entidadeRastreada != null)
            {
                _context.Entry(entidadeRastreada).State = EntityState.Detached;
            }

            // 2. TRUQUE MÁGICO: Anulamos a propriedade de navegação (o objeto inteiro).
            // Isso impede que o EF Core tente rastrear/atualizar o Emitente na tabela dele.
            // A chave estrangeira (documento.EmitenteId) continua intacta!
            documento.Emitente = null!;

            // 3. Atualiza apenas os dados da tabela de Documento
            _context.Documentos.Update(documento);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<List<Documento>> ObterPendentesDeCienciaAsync(Guid empresaId, CancellationToken cancellationToken = default)
        {
            // Retornamos a regra de 10 dias da Sefaz
            var dataLimite = DateTimeOffset.Now.AddDays(-10);

            // 1. Vai no SQLite e traz os resumos pendentes
            var pendentesDoBanco = await _context.Documentos
                .Where(d => d.EmpresaId == empresaId &&
                            d.TipoDocumento == "Resumo" &&
                            d.CienciaEnviada == false)
                .ToListAsync(cancellationToken);

            // 2. Filtra na memória e ORDENA
            var pendentesFiltrados = pendentesDoBanco
                .Where(d => d.DataEmissao >= dataLimite &&
                            !string.IsNullOrEmpty(d.ChaveAcesso) &&
                            !d.ChaveAcesso.StartsWith("SEM_CHAVE") &&
                            !string.IsNullOrEmpty(d.XmlConteudo) &&
                            !d.XmlConteudo.Contains("<cSitNFe>2</cSitNFe>") &&
                            !d.XmlConteudo.Contains("<cSitNFe>3</cSitNFe>"))
                .OrderByDescending(d => d.DataEmissao) // <-- TRUQUE: Processa os recentes primeiro!
                .Take(20)
                .ToList();

            return pendentesFiltrados;
        }
    }
}