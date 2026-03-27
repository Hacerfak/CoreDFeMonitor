// src/CoreDFeMonitor.UI/ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreDFeMonitor.Application.Features.Documentos.Commands;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Application.Features.Documentos.Queries;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty] private int _totalDocumentosMes = 0;
        [ObservableProperty] private int _totalDocumentosHoje = 0;
        [ObservableProperty] private string _ultimaAtualizacao = "Nunca";
        [ObservableProperty] private bool _isCarregandoDados = false;

        // CONTROLES DO BOTÃO DE ATUALIZAÇÃO MANUAL
        [ObservableProperty] private bool _podeAtualizar = true;
        [ObservableProperty] private string _textoBotaoAtualizar = "Sincronizar SEFAZ";

        public ObservableCollection<DocumentoDto> DocumentosRecentes { get; } = new();

        public DashboardViewModel(IMediator mediator, MainViewModel mainViewModel)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;
        }

        public async Task CarregarDadosAsync()
        {
            if (IsCarregandoDados) return;
            IsCarregandoDados = true;

            try
            {
                var totaisResult = await _mediator.Send(new ObterTotaisDocumentosQuery());
                TotalDocumentosMes = totaisResult.TotalMes;
                TotalDocumentosHoje = totaisResult.TotalHoje;

                var listaResult = await _mediator.Send(new ObterUltimosDocumentosQuery());
                DocumentosRecentes.Clear();
                foreach (var doc in listaResult) DocumentosRecentes.Add(doc);

                UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Erro Dashboard: {ex.Message}"); }
            finally { IsCarregandoDados = false; }
        }

        [RelayCommand]
        private async Task AtualizarManualAsync()
        {
            if (!PodeAtualizar) return;

            PodeAtualizar = false;
            TextoBotaoAtualizar = "Buscando...";

            // FORÇA A SINCRONIZAÇÃO MANUAL
            bool rodouComSucesso = await _mediator.Send(new SincronizarDocumentosCommand());

            // Depois que baixar, recarrega as tabelas da tela
            await CarregarDadosAsync();

            if (!rodouComSucesso)
            {
                TextoBotaoAtualizar = "Já em Andamento!";
                await Task.Delay(3000);
            }

            // INICIA O COOLDOWN DE 60 SEGUNDOS SEM TRAVAR A TELA
            _ = IniciarCooldownAsync();
        }

        private async Task IniciarCooldownAsync()
        {
            for (int i = 60; i > 0; i--)
            {
                TextoBotaoAtualizar = $"Aguarde {i}s";
                await Task.Delay(1000);
            }

            TextoBotaoAtualizar = "Sincronizar SEFAZ";
            PodeAtualizar = true;
        }

        [RelayCommand]
        private void AbrirConfiguracoes() => _mainViewModel.NavegarPara<ConfiguracoesViewModel>();

        [RelayCommand]
        private void NavegarDocumentos() => _mainViewModel.NavegarPara<DocumentosViewModel>();
    }
}