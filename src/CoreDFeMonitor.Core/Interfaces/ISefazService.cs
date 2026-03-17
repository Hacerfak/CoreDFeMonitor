// src/CoreDFeMonitor.Core/Interfaces/ISefazService.cs
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface ISefazService
    {
        // Aqui adicionaremos os métodos de Distribuição e Manifestação futuramente
        // Exemplo: Task<RetornoDistribuicao> ConsultarDFeAsync(Empresa empresa, string nsu);

        // Valida se a configuração e o certificado da empresa estão prontos para uso
        bool ValidarConfiguracao(Empresa empresa);
    }
}