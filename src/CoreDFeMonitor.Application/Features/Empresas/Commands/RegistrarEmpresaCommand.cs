using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Empresas.Commands
{
    public class RegistrarEmpresaCommand : IRequest<(bool Sucesso, string Mensagem)>
    {
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string? InscricaoEstadual { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }

        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public long? CodigoMunicipio { get; set; }
        public string? NomeMunicipio { get; set; }
        public string? Cep { get; set; }

        public string CaminhoCertificado { get; set; } = string.Empty;
        public string SenhaCertificado { get; set; } = string.Empty;
    }
}