// src/CoreDFeMonitor.Core/Interfaces/IEmpresaRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface IEmpresaRepository
    {
        Task<Empresa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Empresa?> ObterPorCnpjAsync(string cnpj, CancellationToken cancellationToken = default);
        Task<IEnumerable<Empresa>> ObterTodasAsync(CancellationToken cancellationToken = default);
        Task AdicionarAsync(Empresa empresa, CancellationToken cancellationToken = default);
        Task AtualizarAsync(Empresa empresa, CancellationToken cancellationToken = default);
        Task RemoverAsync(Empresa empresa, CancellationToken cancellationToken = default);
    }
}