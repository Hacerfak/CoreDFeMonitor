using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Documentos.Queries;
using CoreDFeMonitor.Application.Features.Emitentes.Queries;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DocumentosViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;

        // ============================================
        // PAINEL LATERAL (MESTRE) - EMITENTES
        // ============================================
        public ObservableCollection<Emitente> ListaEmitentes { get; } = new();

        [ObservableProperty]
        private Emitente? _emitenteSelecionado;

        // ============================================
        // FILTROS E PAINEL PRINCIPAL (DETALHE)
        // ============================================
        [ObservableProperty] private DateTimeOffset? _dataInicio = DateTimeOffset.Now.AddDays(-30);
        [ObservableProperty] private DateTimeOffset? _dataFim = DateTimeOffset.Now;
        [ObservableProperty] private string _filtroTexto = string.Empty;
        [ObservableProperty] private string _tipoSelecionado = "Todos";

        // Removido o CT-e
        public string[] ListaTipos { get; } = { "Todos", "NF-e", "Eventos" };

        [ObservableProperty] private bool _isCarregando = false;
        [ObservableProperty] private bool _todosSelecionados = false;
        [ObservableProperty] private string _mensagemAcao = string.Empty;

        public ObservableCollection<DocumentoItemViewModel> Documentos { get; } = new();

        public DocumentosViewModel(IMediator mediator, MainViewModel mainViewModel)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;

            _ = InicializarTelaAsync();
        }

        private async Task InicializarTelaAsync()
        {
            await CarregarEmitentesAsync();
            await CarregarDocumentosAsync();
        }

        [RelayCommand]
        public async Task CarregarEmitentesAsync()
        {
            var emitentes = await _mediator.Send(new ObterEmitentesQuery());
            ListaEmitentes.Clear();

            foreach (var emitente in emitentes)
            {
                ListaEmitentes.Add(emitente);
            }
        }

        // Evento automático do CommunityToolkit. Dispara quando o usuário clica num emitente.
        partial void OnEmitenteSelecionadoChanged(Emitente? value)
        {
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
                TipoDocumento = TipoSelecionado,
                EmitenteId = EmitenteSelecionado?.Id // O ID do fornecedor clicado!
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

        partial void OnTodosSelecionadosChanged(bool value)
        {
            foreach (var doc in Documentos) doc.IsSelecionado = value;
        }

        [RelayCommand]
        private async Task BaixarXmlsSelecionadosAsync()
        {
            var selecionados = Documentos.Where(d => d.IsSelecionado).ToList();
            if (!selecionados.Any()) return;

            MensagemAcao = $"Iniciando download de {selecionados.Count} XMLs...";
            await Task.Delay(2000);
            MensagemAcao = string.Empty;
            foreach (var doc in selecionados) doc.IsSelecionado = false;
        }

        [RelayCommand]
        private async Task ManifestarSelecionadosAsync()
        {
            var selecionados = Documentos.Where(d => d.IsSelecionado).ToList();
            if (!selecionados.Any()) return;

            MensagemAcao = $"Enviando evento para {selecionados.Count} notas...";
            await Task.Delay(2000);
            MensagemAcao = string.Empty;
            await CarregarDocumentosAsync();
        }

        [RelayCommand]
        private void NavegarDashboard() => _mainViewModel.NavegarPara<DashboardViewModel>();

        [RelayCommand]
        private void NavegarConfiguracoes() => _mainViewModel.NavegarPara<ConfiguracoesViewModel>();
    }
}