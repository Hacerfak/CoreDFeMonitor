using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Empresas.Commands
{
    public class RegistrarEmpresaCommandHandler : IRequestHandler<RegistrarEmpresaCommand, (bool Sucesso, string Mensagem)>
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ISefazService _sefazService;

        public RegistrarEmpresaCommandHandler(IEmpresaRepository empresaRepository, ISefazService sefazService)
        {
            _empresaRepository = empresaRepository;
            _sefazService = sefazService;
        }

        public async Task<(bool Sucesso, string Mensagem)> Handle(RegistrarEmpresaCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Validar se a empresa já existe
                var empresaExistente = await _empresaRepository.ObterPorCnpjAsync(request.Cnpj, cancellationToken);
                if (empresaExistente != null)
                    return (false, "Já existe uma empresa cadastrada com este CNPJ.");

                // 2. Criar a Entidade (O construtor já valida CNPJ e UF)
                var empresa = new Empresa(
                    request.Cnpj, request.RazaoSocial, request.Uf, request.InscricaoEstadual,
                    request.Logradouro, request.Numero, request.Complemento, request.Bairro,
                    request.CodigoMunicipio, request.NomeMunicipio, request.Cep,
                    request.Telefone, request.Email);

                // 3. Configurar Certificado
                empresa.ConfigurarCertificado(request.CaminhoCertificado, request.SenhaCertificado);

                // 4. Validar o Certificado fisicamente e a configuração do Zeus
                var configValida = _sefazService.ValidarConfiguracao(empresa);
                if (!configValida)
                    return (false, "O Certificado Digital é inválido, está expirado ou a senha está incorreta.");

                // 5. Persistir na base de dados SQLite
                await _empresaRepository.AdicionarAsync(empresa, cancellationToken);

                return (true, "Empresa cadastrada e certificado validado com sucesso!");
            }
            catch (ArgumentException ex)
            {
                // Captura erros de validação da própria entidade (Ex: CNPJ com tamanho errado)
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, $"Erro interno ao registar a empresa: {ex.Message}");
            }
        }
    }
}