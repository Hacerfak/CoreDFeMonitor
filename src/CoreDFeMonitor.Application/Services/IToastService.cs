using System;

namespace CoreDFeMonitor.Application.Services
{
    public enum ToastType
    {
        Info,
        Sucesso,
        Aviso,
        Erro
    }

    public record ToastMessage(Guid Id, string Titulo, string Mensagem, ToastType Tipo, DateTime DataCriacao);

    public interface IToastService
    {
        event Action<ToastMessage>? OnToastAdicionado;
        event Action<Guid>? OnToastRemovido;

        void Exibir(string titulo, string mensagem, ToastType tipo = ToastType.Info);
        void ExibirSucesso(string mensagem);
        void ExibirErro(string mensagem);
        void ExibirAviso(string mensagem);
        void ExibirInfo(string mensagem);
        void Remover(Guid id);
    }
}