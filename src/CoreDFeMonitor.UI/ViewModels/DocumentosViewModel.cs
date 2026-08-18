using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Documentos.Queries;
using CoreDFeMonitor.Application.Features.Emitentes.Queries;
using CoreDFeMonitor.Application.Features.Empresas.Queries;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Application.Features.Documentos.Commands;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Mediator;
using CoreDFeMonitor.Core.Interfaces;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DocumentosViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;
        private readonly IImpressaoService _impressaoService;
        private readonly INotificacaoDesktopService _notificacao;

        // ============================================
        // FILTRO DE EMPRESA (DESTINATÁRIO)
        // ============================================
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

        // ============================================
        // FILTRO LATERAL (EMITENTES)
        // ============================================
        public ObservableCollection<Emitente> ListaEmitentes { get; } = new();

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

        // ============================================
        // FILTROS SUPERIORES E CONTROLES DE TELA
        // ============================================
        [ObservableProperty] private DateTimeOffset? _dataInicio = DateTimeOffset.Now.AddDays(-30);
        [ObservableProperty] private DateTimeOffset? _dataFim = DateTimeOffset.Now;
        [ObservableProperty] private string _filtroTexto = string.Empty;

        [ObservableProperty] private string _tipoSelecionado = "Todos";
        public string[] ListaTipos { get; } = { "Todos", "Resumo", "NFe", "Evento" };

        [ObservableProperty] private bool _isCarregando = false;
        [ObservableProperty] private string _mensagemAcao = string.Empty;

        // Propriedade manual para o checkbox de "Selecionar Todos"
        private bool _todosSelecionados = false;
        public bool TodosSelecionados
        {
            get => _todosSelecionados;
            set
            {
                if (SetProperty(ref _todosSelecionados, value))
                {
                    foreach (var doc in Documentos) doc.IsSelecionado = value;
                }
            }
        }

        public ObservableCollection<DocumentoItemViewModel> Documentos { get; } = new();

        public DocumentosViewModel(IMediator mediator, MainViewModel mainViewModel, IImpressaoService impressaoService, INotificacaoDesktopService notificacao)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;
            _impressaoService = impressaoService;
            _notificacao = notificacao;
            _ = InicializarTelaAsync();
        }

        private async Task InicializarTelaAsync()
        {
            var empresas = await _mediator.Send(new ObterTodasEmpresasQuery());
            foreach (var emp in empresas) ListaEmpresas.Add(emp);

            // Só seleciona a primeira empresa para não disparar chamadas nulas
            if (ListaEmpresas.Any())
            {
                _empresaSelecionada = ListaEmpresas.First();
                OnPropertyChanged(nameof(EmpresaSelecionada));
            }

            var emitentes = await _mediator.Send(new ObterEmitentesQuery());
            foreach (var emitente in emitentes) ListaEmitentes.Add(emitente);

            await CarregarDocumentosAsync();
        }

        [RelayCommand]
        public async Task CarregarDocumentosAsync()
        {
            IsCarregando = true;
            MensagemAcao = string.Empty;

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
            foreach (var doc in resultados) Documentos.Add(new DocumentoItemViewModel(doc));

            // Reseta a checkbox sem disparar o loop
            _todosSelecionados = false;
            OnPropertyChanged(nameof(TodosSelecionados));

            IsCarregando = false;
        }

        [RelayCommand]
        private void NavegarDashboard() => _mainViewModel.NavegarPara<DashboardViewModel>();

        [RelayCommand]
        private void NavegarConfiguracoes() => _mainViewModel.NavegarPara<ConfiguracoesViewModel>();

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
        private void ImprimirDanfe(DocumentoListagemDto doc)
        {
            if (doc.SchemaDisplay == "NFe" || doc.SchemaDisplay == "NF-e Completa")
            {
                try
                {
                    _impressaoService.VisualizarDanfe(doc.XmlConteudo, doc.ChaveAcesso);
                }
                catch (Exception ex)
                {
                    _notificacao.Exibir("Erro de Impressão", ex.Message);
                }
            }
            else
            {
                _notificacao.Exibir("Aviso", "Apenas a NF-e completa (com protocolo) pode gerar o DANFE.");
            }
        }

        [RelayCommand]
        private async Task ConsultarStatusNFeAsync(DocumentoListagemDto doc)
        {
            IsCarregando = true;
            try
            {
                // Substitua 'ConsultarStatusDocumentoCommand' pelo nome exato que você criou no CQRS
                var result = await _mediator.Send(new CoreDFeMonitor.Application.Features.Documentos.Commands.ConsultarStatusDocumentoCommand
                {
                    DocumentoId = doc.Id
                });
                _notificacao.Exibir("Retorno SEFAZ", result.Mensagem);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        private async Task ManifestarConfirmacaoAsync(DocumentoListagemDto doc)
        {
            await ExecutarManifestacaoAsync(doc, 210200, "Confirmação");
        }

        [RelayCommand]
        private async Task ManifestarDesconhecimentoAsync(DocumentoListagemDto doc)
        {
            await ExecutarManifestacaoAsync(doc, 210220, "Desconhecimento");
        }

        [RelayCommand]
        private async Task ManifestarNaoRealizadaAsync(DocumentoListagemDto doc)
        {
            if (doc == null) return;

            // Tratamento de segurança para UI: Se você ainda não possui um serviço 
            // de Dialog customizado no Avalonia que roda de forma assíncrona segura,
            // utilize uma justificativa padrão válida (mínimo 15 caracteres) 
            // para evitar o crash da Thread de UI.
            string justificativaSegura = "Operacao nao realizada pela empresa";

            await ExecutarManifestacaoAsync(doc, 210240, justificativaSegura);
        }

        [RelayCommand]
        private async Task EnviarCienciaManualAsync(DocumentoListagemDto doc)
        {
            if (doc == null) return;

            // O código 210210 é o Evento de Ciência da Operação
            await ExecutarManifestacaoAsync(doc, 210210, "Ciencia da Operacao");
        }

        private async Task ExecutarManifestacaoAsync(DocumentoListagemDto doc, int codigoEvento, string justificativa)
        {
            if (IsCarregando) return; // Trava contra cliques duplos que causam crash

            IsCarregando = true;
            try
            {
                var result = await _mediator.Send(new CoreDFeMonitor.Application.Features.Documentos.Commands.ManifestarDocumentoCommand
                {
                    DocumentoId = doc.Id,
                    CodigoManifestacao = codigoEvento,
                    Justificativa = justificativa
                });

                _notificacao.Exibir("Manifestação Sefaz", result.Mensagem);

                if (result.Sucesso)
                {
                    await CarregarDocumentosAsync();
                }
            }
            catch (Exception ex)
            {
                _notificacao.Exibir("Erro CRÍTICO", $"Ocorreu um erro ao manifestar: {ex.Message}");
            }
            finally
            {
                IsCarregando = false;
            }
        }
    }
}