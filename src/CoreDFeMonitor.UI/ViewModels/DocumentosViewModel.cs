using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Documentos.Commands;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Application.Features.Documentos.Queries;
using CoreDFeMonitor.Application.Features.Emitentes.Queries;
using CoreDFeMonitor.Application.Features.Empresas.Queries;
using CoreDFeMonitor.Application.Services;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DocumentosViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;
        private readonly IImpressaoService _impressaoService;
        private readonly IToastService _toastService;

        // Lista de Empresas (Destinatários)
        public ObservableCollection<Empresa> ListaEmpresas { get; } = new();
        private Empresa? _empresaSelecionada;
        public Empresa? EmpresaSelecionada
        {
            get => _empresaSelecionada;
            set
            {
                if (SetProperty(ref _empresaSelecionada, value))
                {
                    _ = CarregarDocumentosAsync();
                }
            }
        }

        // Filtro e Lista de Fornecedores / Emitentes
        public ObservableCollection<Emitente> ListaEmitentesOriginal { get; } = new();
        public ObservableCollection<Emitente> ListaEmitentesFiltrada { get; } = new();

        [ObservableProperty] private string _buscaEmitenteTexto = string.Empty;
        partial void OnBuscaEmitenteTextoChanged(string value) => FiltrarListaEmitentes();

        private Emitente? _emitenteSelecionado;
        public Emitente? EmitenteSelecionado
        {
            get => _emitenteSelecionado;
            set
            {
                if (SetProperty(ref _emitenteSelecionado, value))
                {
                    _ = CarregarDocumentosAsync();
                }
            }
        }

        // Filtros Superiores
        [ObservableProperty] private DateTimeOffset? _dataInicio = DateTimeOffset.Now.AddDays(-30);
        [ObservableProperty] private DateTimeOffset? _dataFim = DateTimeOffset.Now;
        [ObservableProperty] private string _filtroTexto = string.Empty;
        [ObservableProperty] private string _tipoSelecionado = "Todos";
        public string[] ListaTipos { get; } = { "Todos", "Resumo", "NFe", "Evento" };

        [ObservableProperty] private bool _isCarregando = false;
        [ObservableProperty] private string _chaveDownloadInput = string.Empty;
        [ObservableProperty] private bool _mostrarModalChave = false;

        // Controle de Seleção em Massa
        private bool _todosSelecionados;
        public bool TodosSelecionados
        {
            get => _todosSelecionados;
            set
            {
                if (SetProperty(ref _todosSelecionados, value))
                {
                    foreach (var doc in Documentos) doc.IsSelecionado = value;
                    AtualizarContagemSelecionados();
                }
            }
        }

        [ObservableProperty] private int _totalSelecionados = 0;
        public ObservableCollection<DocumentoItemViewModel> Documentos { get; } = new();

        public DocumentosViewModel(IMediator mediator, MainViewModel mainViewModel, IImpressaoService impressaoService, IToastService toastService)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;
            _impressaoService = impressaoService;
            _toastService = toastService;
            _ = InicializarTelaAsync();
        }

        private async Task InicializarTelaAsync()
        {
            var empresas = await _mediator.Send(new ObterTodasEmpresasQuery());
            ListaEmpresas.Clear();
            foreach (var emp in empresas) ListaEmpresas.Add(emp);

            if (ListaEmpresas.Any())
            {
                _empresaSelecionada = ListaEmpresas.First();
                OnPropertyChanged(nameof(EmpresaSelecionada));
            }

            var emitentes = await _mediator.Send(new ObterEmitentesQuery());
            ListaEmitentesOriginal.Clear();
            foreach (var emit in emitentes) ListaEmitentesOriginal.Add(emit);
            FiltrarListaEmitentes();

            await CarregarDocumentosAsync();
        }

        private void FiltrarListaEmitentes()
        {
            ListaEmitentesFiltrada.Clear();
            var termo = BuscaEmitenteTexto?.Trim().ToLower() ?? string.Empty;
            var filtrados = string.IsNullOrEmpty(termo)
                ? ListaEmitentesOriginal
                : ListaEmitentesOriginal.Where(e => e.RazaoSocial.ToLower().Contains(termo) || e.Cnpj.Contains(termo));

            foreach (var emit in filtrados) ListaEmitentesFiltrada.Add(emit);
        }

        [RelayCommand]
        public async Task CarregarDocumentosAsync()
        {
            IsCarregando = true;
            try
            {
                var query = new ObterDocumentosQuery
                {
                    EmpresaId = EmpresaSelecionada?.Id,
                    EmitenteId = EmitenteSelecionado?.Id,
                    DataInicio = DataInicio?.DateTime,
                    DataFim = DataFim?.DateTime,
                    FiltroTexto = FiltroTexto,
                    TipoDocumento = TipoSelecionado
                };

                var resultados = await _mediator.Send(query);
                Documentos.Clear();
                foreach (var doc in resultados)
                {
                    var itemVm = new DocumentoItemViewModel(doc);
                    itemVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DocumentoItemViewModel.IsSelecionado))
                            AtualizarContagemSelecionados();
                    };
                    Documentos.Add(itemVm);
                }

                _todosSelecionados = false;
                OnPropertyChanged(nameof(TodosSelecionados));
                AtualizarContagemSelecionados();
            }
            finally
            {
                IsCarregando = false;
            }
        }

        private void AtualizarContagemSelecionados()
        {
            TotalSelecionados = Documentos.Count(d => d.IsSelecionado);
        }

        // ============================================
        // AÇÕES EM MASSA (BULK)
        // ============================================
        [RelayCommand]
        private async Task ManifestarConfirmacaoEmMassaAsync()
        {
            var selecionados = Documentos.Where(d => d.IsSelecionado && d.Dados.PodeManifestar).ToList();
            if (!selecionados.Any())
            {
                _toastService.ExibirAviso("Nenhuma NF-e válida selecionada para confirmação.");
                return;
            }

            IsCarregando = true;
            int sucessos = 0;
            foreach (var item in selecionados)
            {
                var result = await _mediator.Send(new ManifestarDocumentoCommand
                {
                    DocumentoId = item.Dados.Id,
                    CodigoManifestacao = 210200,
                    Justificativa = "Confirmacao em Lote"
                });
                if (result.Sucesso) sucessos++;
            }

            IsCarregando = false;
            _toastService.ExibirSucesso($"{sucessos} de {selecionados.Count} notas foram confirmadas!");
            await CarregarDocumentosAsync();
        }

        [RelayCommand]
        private async Task CopiarChavesSelecionadasAsync()
        {
            var chaves = Documentos.Where(d => d.IsSelecionado).Select(d => d.Dados.ChaveAcesso);
            if (!chaves.Any()) return;

            string texto = string.Join(Environment.NewLine, chaves);
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(texto);
                _toastService.ExibirSucesso($"{chaves.Count()} chave(s) copiada(s) para a área de transferência!");
            }
        }

        // ============================================
        // DOWNLOAD POR CHAVE
        // ============================================
        [RelayCommand]
        private void AbrirModalDownloadChave() => MostrarModalChave = true;

        [RelayCommand]
        private void FecharModalDownloadChave()
        {
            MostrarModalChave = false;
            ChaveDownloadInput = string.Empty;
        }

        [RelayCommand]
        private async Task ProcessarDownloadPorChaveAsync()
        {
            if (EmpresaSelecionada == null)
            {
                _toastService.ExibirAviso("Selecione uma empresa destinatária primeiro.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ChaveDownloadInput) || ChaveDownloadInput.Trim().Length != 44)
            {
                _toastService.ExibirErro("Digite uma Chave de Acesso válida com 44 números.");
                return;
            }

            IsCarregando = true;
            try
            {
                var result = await _mediator.Send(new BaixarXmlPorChaveCommand
                {
                    EmpresaId = EmpresaSelecionada.Id,
                    ChaveAcesso = ChaveDownloadInput.Trim()
                });

                if (result.Sucesso)
                {
                    _toastService.ExibirSucesso($"SEFAZ: {result.Mensagem}");
                    FecharModalDownloadChave();
                    await CarregarDocumentosAsync();
                }
                else
                {
                    _toastService.ExibirErro(result.Mensagem);
                }
            }
            finally
            {
                IsCarregando = false;
            }
        }

        // ============================================
        // AÇÕES INDIVIDUAIS & CÓPIA
        // ============================================
        [RelayCommand]
        private async Task CopiarTextoAsync(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(texto);
                _toastService.ExibirInfo("Texto copiado!");
            }
        }

        [RelayCommand]
        private void ImprimirDanfe(DocumentoListagemDto doc)
        {
            if (doc.IsNFe)
            {
                try
                {
                    _impressaoService.VisualizarDanfe(doc.XmlConteudo, doc.ChaveAcesso);
                }
                catch (Exception ex)
                {
                    _toastService.ExibirErro($"Erro na impressão: {ex.Message}");
                }
            }
            else
            {
                _toastService.ExibirAviso("Apenas NF-e completa (procNFe) possui layout de DANFE.");
            }
        }

        [RelayCommand]
        private async Task ConsultarStatusNFeAsync(DocumentoListagemDto doc)
        {
            IsCarregando = true;
            try
            {
                var result = await _mediator.Send(new ConsultarStatusDocumentoCommand { DocumentoId = doc.Id });
                _toastService.ExibirInfo(result.Mensagem);
            }
            finally { IsCarregando = false; }
        }

        [RelayCommand]
        private async Task ManifestarConfirmacaoAsync(DocumentoListagemDto doc) => await ExecutarManifestacaoAsync(doc, 210200, "Confirmação");

        [RelayCommand]
        private async Task ManifestarDesconhecimentoAsync(DocumentoListagemDto doc) => await ExecutarManifestacaoAsync(doc, 210220, "Desconhecimento");

        [RelayCommand]
        private async Task ManifestarNaoRealizadaAsync(DocumentoListagemDto doc) => await ExecutarManifestacaoAsync(doc, 210240, "Operacao nao realizada pela empresa");

        [RelayCommand]
        private async Task EnviarCienciaManualAsync(DocumentoListagemDto doc) => await ExecutarManifestacaoAsync(doc, 210210, "Ciencia da Operacao");

        private async Task ExecutarManifestacaoAsync(DocumentoListagemDto doc, int codigoEvento, string justificativa)
        {
            if (IsCarregando) return;
            IsCarregando = true;
            try
            {
                var result = await _mediator.Send(new ManifestarDocumentoCommand
                {
                    DocumentoId = doc.Id,
                    CodigoManifestacao = codigoEvento,
                    Justificativa = justificativa
                });

                if (result.Sucesso)
                {
                    _toastService.ExibirSucesso(result.Mensagem);
                    await CarregarDocumentosAsync();
                }
                else
                {
                    _toastService.ExibirErro(result.Mensagem);
                }
            }
            catch (Exception ex)
            {
                _toastService.ExibirErro($"Erro ao manifestar: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand] private void NavegarDashboard() => _mainViewModel.NavegarPara<DashboardViewModel>();
        [RelayCommand] private void NavegarConfiguracoes() => _mainViewModel.NavegarPara<ConfiguracoesViewModel>();
    }
}