// src/CoreDFeMonitor.Core/Interfaces/ISefazService.cs
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;

namespace CoreDFeMonitor.Core.Interfaces
{
    public record SefazCadastroResult(
        bool Sucesso,
        string Cnpj,
        string RazaoSocial,
        string? InscricaoEstadual,
        string? Logradouro,
        string? Numero,
        string? Complemento,
        string? Bairro,
        long? CodigoMunicipio,
        string? NomeMunicipio,
        string? Cep,
        string MensagemErro
    );

    public interface ISefazService
    {
        bool ValidarConfiguracao(Empresa empresa);
        Task<SefazCadastroResult> ConsultarCadastroAsync(string uf, string caminhoCertificado, string senha);
    }
}