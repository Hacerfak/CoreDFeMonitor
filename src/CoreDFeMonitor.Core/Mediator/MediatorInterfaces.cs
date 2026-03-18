namespace CoreDFeMonitor.Core.Mediator
{
    // A interface vazia que marca um objeto como um "Comando" ou "Pedido"
    public interface IRequest<TResponse> { }

    // O contrato que obriga o Handler a implementar o método Handle
    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    // A interface do nosso "Carteiro" (O Mediator)
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}