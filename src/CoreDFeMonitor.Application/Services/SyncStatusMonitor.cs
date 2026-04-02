// src/CoreDFeMonitor.Application/Services/SyncStatusMonitor.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CoreDFeMonitor.Application.Services
{
    public interface ISyncStatusMonitor : INotifyPropertyChanged
    {
        bool EmExecucao { get; }
        DateTimeOffset? UltimaSincronizacao { get; }
        DateTimeOffset? ProximaSincronizacao { get; }
        string MensagemStatus { get; }

        void IniciarSincronizacao(string mensagem);
        void FinalizarSincronizacao(string mensagem, DateTimeOffset proxima);
        void AtualizarMensagem(string mensagem);
        event EventHandler SincronizacaoConcluida;
    }

    public class SyncStatusMonitor : ISyncStatusMonitor
    {
        private bool _emExecucao;
        private DateTimeOffset? _ultimaSincronizacao;
        private DateTimeOffset? _proximaSincronizacao;
        private string _mensagemStatus = "Aguardando inicialização...";

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? SincronizacaoConcluida;

        public bool EmExecucao
        {
            get => _emExecucao;
            private set { _emExecucao = value; OnPropertyChanged(); }
        }

        public DateTimeOffset? UltimaSincronizacao
        {
            get => _ultimaSincronizacao;
            private set { _ultimaSincronizacao = value; OnPropertyChanged(); }
        }

        public DateTimeOffset? ProximaSincronizacao
        {
            get => _proximaSincronizacao;
            private set { _proximaSincronizacao = value; OnPropertyChanged(); }
        }

        public string MensagemStatus
        {
            get => _mensagemStatus;
            private set { _mensagemStatus = value; OnPropertyChanged(); }
        }

        public void IniciarSincronizacao(string mensagem)
        {
            EmExecucao = true;
            MensagemStatus = mensagem;
            ProximaSincronizacao = null; // Limpa a próxima pois está rodando agora
        }

        public void AtualizarMensagem(string mensagem)
        {
            MensagemStatus = mensagem;
        }

        public void FinalizarSincronizacao(string mensagem, DateTimeOffset proxima)
        {
            EmExecucao = false;
            MensagemStatus = mensagem;
            UltimaSincronizacao = DateTimeOffset.Now;
            ProximaSincronizacao = proxima;
            SincronizacaoConcluida?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}