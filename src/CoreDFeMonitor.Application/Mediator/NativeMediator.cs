using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Mediator
{
    public class NativeMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public NativeMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();

            // Descobre em tempo de execução qual é a interface IRequestHandler vinculada a este request
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

            // Pede ao .NET para instanciar o Handler correto
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"Nenhum Handler foi registrado para o comando {requestType.Name}.");

            // Executa o método Handle
            var method = handlerType.GetMethod("Handle");
            return (Task<TResponse>)method!.Invoke(handler, new object[] { request, cancellationToken })!;
        }
    }
}