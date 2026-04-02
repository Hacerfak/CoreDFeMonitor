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
using CoreDFeMonitor.Application.Services;
using Avalonia.Threading;

namespace CoreDFeMonitor.UI.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly MainViewModel _mainViewModel;
        public ISyncStatusMonitor SyncStatus { get; }

        [ObservableProperty] private int _totalDocumentosMes = 0;
        [ObservableProperty] private int _totalDocumentosHoje = 0;
        [ObservableProperty] private string _ultimaAtualizacao = "Nunca";
        [ObservableProperty] private bool _isCarregandoDados = false;

        // CONTROLES DO BOTÃO DE ATUALIZAÇÃO MANUAL
        [ObservableProperty] private bool _podeAtualizar = true;
        [ObservableProperty] private string _textoBotaoAtualizar = "Sincronizar SEFAZ";

        public ObservableCollection<DocumentoDto> DocumentosRecentes { get; } = new();

        public DashboardViewModel(IMediator mediator, MainViewModel mainViewModel, ISyncStatusMonitor syncStatus)
        {
            _mediator = mediator;
            _mainViewModel = mainViewModel;
            SyncStatus = syncStatus;

            SyncStatus.SincronizacaoConcluida += OnSincronizacaoConcluida;
        }

        private void OnSincronizacaoConcluida(object? sender, EventArgs e)
        {
            // Pede à Thread principal da Interface Gráfica (UI) para recarregar os dados
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await CarregarDadosAsync();
            });
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
            TextoBotaoAtualizar = "Sincronizando...";

            // 1. Proteção: Se o Worker automático já estiver rodando neste exato segundo, ignora o clique!
            if (SyncStatus.EmExecucao)
            {
                return;
            }

            try
            {
                // 2. Liga a barra de progresso e muda o texto no Dashboard
                SyncStatus.IniciarSincronizacao("Sincronização manual iniciada pelo usuário...");

                // 3. Dispara o motor da Sefaz
                // FORÇA A SINCRONIZAÇÃO MANUAL
                bool executouComSucesso = await _mediator.Send(new SincronizarDocumentosCommand());

                if (!executouComSucesso)
                {
                    SyncStatus.AtualizarMensagem("O sincronização já estava ocorrendo.");
                }

                // Depois que baixar, recarrega as tabelas da tela
                await CarregarDadosAsync();

                // INICIA O COOLDOWN DE 60 SEGUNDOS SEM TRAVAR A TELA
                _ = IniciarCooldownAsync();
            }
            catch (Exception ex)
            {
                SyncStatus.AtualizarMensagem($"Erro na sincronização manual: {ex.Message}");
            }
            finally
            {
                // 4. Desliga a barra de progresso, atualiza a "Última Sincronização" para AGORA
                // e joga a próxima para +30 minutos visualmente.
                SyncStatus.FinalizarSincronizacao("Sincronização manual concluída.", DateTimeOffset.Now.AddMinutes(2));
            }
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