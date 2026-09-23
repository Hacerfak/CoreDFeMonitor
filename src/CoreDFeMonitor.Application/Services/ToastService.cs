using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreDFeMonitor.Application.Services
{
    public class ToastService : IToastService
    {
        public event Action<ToastMessage>? OnToastAdicionado;
        public event Action<Guid>? OnToastRemovido;

        public void Exibir(string titulo, string mensagem, ToastType tipo = ToastType.Info)
        {
            var toast = new ToastMessage(Guid.NewGuid(), titulo, mensagem, tipo, DateTime.Now);
            OnToastAdicionado?.Invoke(toast);

            // Auto-descarte após 4 segundos
            _ = Task.Run(async () =>
            {
                await Task.Delay(4000);
                OnToastRemovido?.Invoke(toast.Id);
            });
        }

        public void ExibirSucesso(string mensagem) => Exibir("Sucesso", mensagem, ToastType.Sucesso);
        public void ExibirErro(string mensagem) => Exibir("Atenção", mensagem, ToastType.Erro);
        public void ExibirAviso(string mensagem) => Exibir("Aviso", mensagem, ToastType.Aviso);
        public void ExibirInfo(string mensagem) => Exibir("Informação", mensagem, ToastType.Info);

        public void Remover(Guid id) => OnToastRemovido?.Invoke(id);
    }
}