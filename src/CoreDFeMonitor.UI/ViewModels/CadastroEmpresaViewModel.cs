// src/CoreDFeMonitor.UI/ViewModels/CadastroEmpresaViewModel.cs
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Empresas.Commands;
using MediatR;
using Avalonia.Platform.Storage; // Para selecionar ficheiros

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class CadastroEmpresaViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IStorageProvider _storageProvider; // Injetado para abrir a janela de ficheiros

        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _razaoSocial = string.Empty;
        [ObservableProperty] private string _uf = string.Empty;
        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;

        [ObservableProperty] private string _mensagemStatus = string.Empty;
        [ObservableProperty] private bool _isCarregando = false;

        public CadastroEmpresaViewModel(IMediator mediator, IStorageProvider storageProvider)
        {
            _mediator = mediator;
            _storageProvider = storageProvider;
        }

        [RelayCommand]
        public async Task SelecionarCertificadoAsync()
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Selecione o Certificado Digital (A1)",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Certificado PFX") { Patterns = new[] { "*.pfx", "*.p12" } } }
            };

            var file = await _storageProvider.OpenFilePickerAsync(options);
            if (file.Count > 0)
            {
                CaminhoCertificado = file[0].Path.LocalPath;
            }
        }

        [RelayCommand]
        public async Task SalvarEmpresaAsync()
        {
            IsCarregando = true;
            MensagemStatus = "Validando certificado...";

            var command = new RegistrarEmpresaCommand
            {
                Cnpj = Cnpj,
                RazaoSocial = RazaoSocial,
                Uf = Uf,
                CaminhoCertificado = CaminhoCertificado,
                SenhaCertificado = SenhaCertificado
            };

            // Envia para o Handler na camada Application
            var result = await _mediator.Send(command);

            MensagemStatus = result.Mensagem;
            IsCarregando = false;

            if (result.Sucesso)
            {
                // Limpar campos após sucesso ou navegar de volta para o Dashboard
                Cnpj = ""; RazaoSocial = ""; Uf = ""; CaminhoCertificado = ""; SenhaCertificado = "";
            }
        }
    }
}