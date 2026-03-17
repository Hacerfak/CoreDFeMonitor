// src/CoreDFeMonitor.Infrastructure/Data/Repositories/EmpresaRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data.Repositories
{
    public class EmpresaRepository : IEmpresaRepository
    {
        private readonly DFeMonitorDbContext _context;

        public EmpresaRepository(DFeMonitorDbContext context)
        {
            _context = context;
        }

        public async Task<Empresa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Empresas.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<Empresa?> ObterPorCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        {
            return await _context.Empresas.FirstOrDefaultAsync(e => e.Cnpj == cnpj, cancellationToken);
        }

        public async Task<IEnumerable<Empresa>> ObterTodasAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Empresas.ToListAsync(cancellationToken);
        }

        public async Task AdicionarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            await _context.Empresas.AddAsync(empresa, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AtualizarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            _context.Empresas.Update(empresa);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoverAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}