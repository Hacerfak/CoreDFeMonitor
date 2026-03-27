// src/CoreDFeMonitor.UI/ViewModels/DocumentosViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Documentos.Queries;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DocumentosViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;

        // Filtros
        [ObservableProperty] private DateTimeOffset? _dataInicio = DateTimeOffset.Now.AddDays(-30);
        [ObservableProperty] private DateTimeOffset? _dataFim = DateTimeOffset.Now;
        [ObservableProperty] private string _filtroTexto = string.Empty;
        [ObservableProperty] private string _tipoSelecionado = "Todos";
        public string[] ListaTipos { get; } = { "Todos", "NF-e", "Eventos" };

        [ObservableProperty] private bool _isCarregando = false;
        [ObservableProperty] private bool _todosSelecionados = false;
        [ObservableProperty] private string _mensagemAcao = string.Empty;

        // Tabela
        public ObservableCollection<DocumentoItemViewModel> Documentos { get; } = new();

        public DocumentosViewModel(IMediator mediator, MainViewModel mainViewModel)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;
            _ = CarregarDocumentosAsync();
        }

        [RelayCommand]
        public async Task CarregarDocumentosAsync()
        {
            IsCarregando = true;
            MensagemAcao = string.Empty;

            var query = new ObterDocumentosQuery
            {
                DataInicio = DataInicio?.DateTime,
                DataFim = DataFim?.DateTime,
                FiltroTexto = FiltroTexto,
                TipoDocumento = TipoSelecionado
            };

            var resultados = await _mediator.Send(query);

            Documentos.Clear();
            foreach (var doc in resultados)
            {
                Documentos.Add(new DocumentoItemViewModel(doc));
            }

            TodosSelecionados = false;
            IsCarregando = false;
        }

        // AÇÕES EM MASSA
        partial void OnTodosSelecionadosChanged(bool value)
        {
            foreach (var doc in Documentos) doc.IsSelecionado = value;
        }

        [RelayCommand]
        private async Task BaixarXmlsSelecionadosAsync()
        {
            var selecionados = Documentos.Where(d => d.IsSelecionado).ToList();
            if (!selecionados.Any()) return;

            MensagemAcao = $"Iniciando download de {selecionados.Count} XMLs (em desenvolvimento...)";
            await Task.Delay(2000); // MOC da ação real
            MensagemAcao = string.Empty;
            foreach (var doc in selecionados) doc.IsSelecionado = false;
        }

        [RelayCommand]
        private async Task ManifestarSelecionadosAsync()
        {
            var selecionados = Documentos.Where(d => d.IsSelecionado).ToList();
            if (!selecionados.Any()) return;

            MensagemAcao = $"Enviando evento para {selecionados.Count} notas (em desenvolvimento...)";
            await Task.Delay(2000);
            MensagemAcao = string.Empty;
            await CarregarDocumentosAsync();
        }

        // NAVEGAÇÃO
        [RelayCommand]
        private void NavegarDashboard() => _mainViewModel.NavegarPara<DashboardViewModel>();

        [RelayCommand]
        private void NavegarConfiguracoes() => _mainViewModel.NavegarPara<ConfiguracoesViewModel>();
    }
}