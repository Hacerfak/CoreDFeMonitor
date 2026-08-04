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

    public record DocumentoZip(string Nsu, string Schema, string XmlDescompactado);
    public record SefazDistribuicaoResult(bool Sucesso, string UltimoNsuRetornado, string Mensagem, List<DocumentoZip> Documentos);
    public record SefazManifestacaoResult(bool Sucesso, string Mensagem);

    public interface ISefazService
    {
        bool ValidarConfiguracao(Empresa empresa);
        Task<SefazCadastroResult> ConsultarCadastroAsync(string uf, string caminhoCertificado, string senha);

        // Apenas a distribuição de NF-e e eventos
        Task<SefazDistribuicaoResult> BaixarDocumentosAsync(Empresa empresa);
        Task<SefazManifestacaoResult> EnviarCienciaOperacaoAsync(Empresa empresa, string chaveAcesso);
    }
}