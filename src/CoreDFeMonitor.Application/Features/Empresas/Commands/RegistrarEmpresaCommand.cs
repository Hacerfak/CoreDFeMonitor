// src/CoreDFeMonitor.Application/Features/Empresas/Commands/RegistrarEmpresaCommand.cs
using MediatR;

namespace CoreDFeMonitor.Application.Features.Empresas.Commands
{
    // O retorno (bool, string) indica Sucesso e uma Mensagem de Erro/Sucesso
    public class RegistrarEmpresaCommand : IRequest<(bool Sucesso, string Mensagem)>
    {
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string CaminhoCertificado { get; set; } = string.Empty;
        public string SenhaCertificado { get; set; } = string.Empty;
    }
}