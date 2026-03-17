// src/CoreDFeMonitor.UI/ViewModels/CadastroEmpresaViewModel.cs
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Empresas.Commands;
using CoreDFeMonitor.Core.Interfaces;
using MediatR;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class CadastroEmpresaViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly ISefazService _sefazService;

        // Lista de UFs para o ComboBox
        public string[] ListaUFs { get; } = { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };

        // Fase 1: Credenciais
        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private string _uf = "SP"; // Padrão selecionado

        // Fase 2: Dados Retornados (Revisão)
        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _razaoSocial = string.Empty;
        [ObservableProperty] private string _inscricaoEstadual = string.Empty;
        [ObservableProperty] private string _telefone = string.Empty;
        [ObservableProperty] private string _email = string.Empty;

        [ObservableProperty] private string? _logradouro;
        [ObservableProperty] private string? _numero;
        [ObservableProperty] private string? _complemento;
        [ObservableProperty] private string? _bairro;
        [ObservableProperty] private string? _nomeMunicipio;
        [ObservableProperty] private long? _codigoMunicipio;
        [ObservableProperty] private string? _cep;

        // Controle de UI
        [ObservableProperty] private bool _isFaseRevisao = false;
        [ObservableProperty] private bool _isCarregando = false;
        [ObservableProperty] private string _mensagemStatus = string.Empty;

        public CadastroEmpresaViewModel(IMediator mediator, ISefazService sefazService)
        {
            _mediator = mediator;
            _sefazService = sefazService;
        }

        [RelayCommand]
        public async Task SelecionarCertificadoAsync()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione o Certificado Digital (A1)",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Certificado PFX") { Patterns = new[] { "*.pfx", "*.p12" } } }
                });

                if (files.Count > 0)
                {
                    CaminhoCertificado = files[0].Path.LocalPath;
                }
            }
        }

        [RelayCommand]
        public async Task ConsultarSefazAsync()
        {
            if (string.IsNullOrWhiteSpace(CaminhoCertificado) || string.IsNullOrWhiteSpace(SenhaCertificado))
            {
                MensagemStatus = "Selecione o certificado e digite a senha.";
                return;
            }

            IsCarregando = true;
            MensagemStatus = "Extraindo CNPJ e consultando Sefaz...";

            var resultado = await _sefazService.ConsultarCadastroAsync(Uf, CaminhoCertificado, SenhaCertificado);

            if (resultado.Sucesso)
            {
                Cnpj = resultado.Cnpj;
                RazaoSocial = resultado.RazaoSocial;
                InscricaoEstadual = resultado.InscricaoEstadual ?? "";
                Logradouro = resultado.Logradouro;
                Numero = resultado.Numero;
                Complemento = resultado.Complemento;
                Bairro = resultado.Bairro;
                NomeMunicipio = resultado.NomeMunicipio;
                CodigoMunicipio = resultado.CodigoMunicipio;
                Cep = resultado.Cep;

                MensagemStatus = string.Empty;
                IsFaseRevisao = true; // Avança para o Passo 2
            }
            else
            {
                MensagemStatus = resultado.MensagemErro;
            }

            IsCarregando = false;
        }

        [RelayCommand]
        public void VoltarFase1()
        {
            IsFaseRevisao = false;
            MensagemStatus = string.Empty;
        }

        [RelayCommand]
        public async Task SalvarEmpresaAsync()
        {
            IsCarregando = true;
            MensagemStatus = "Salvando no Banco de Dados...";

            var command = new RegistrarEmpresaCommand
            {
                Cnpj = Cnpj,
                RazaoSocial = RazaoSocial,
                Uf = Uf,
                InscricaoEstadual = InscricaoEstadual,
                Logradouro = Logradouro,
                Numero = Numero,
                Complemento = Complemento,
                Bairro = Bairro,
                CodigoMunicipio = CodigoMunicipio,
                NomeMunicipio = NomeMunicipio,
                Cep = Cep,
                Telefone = Telefone,
                Email = Email,
                CaminhoCertificado = CaminhoCertificado,
                SenhaCertificado = SenhaCertificado
            };

            var result = await _mediator.Send(command);

            MensagemStatus = result.Mensagem;
            IsCarregando = false;

            if (result.Sucesso)
            {
                // Limpa ou navega para o Dashboard
                VoltarFase1();
                CaminhoCertificado = ""; SenhaCertificado = "";
            }
        }
    }
}