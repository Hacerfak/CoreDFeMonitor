// src/CoreDFeMonitor.Core/Interfaces/IArmazenamentoXmlService.cs
using System.Threading.Tasks;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface IArmazenamentoXmlService
    {
        Task SalvarXmlAsync(string cnpjEmpresa, string chaveAcesso, string schema, string xmlConteudo);
    }
}