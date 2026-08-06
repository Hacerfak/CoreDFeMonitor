// Caminho: src/CoreDFeMonitor.Worker/Worker.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Features.Documentos.Commands;
using CoreDFeMonitor.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de Sincronização iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker acordou: Solicitando Sincronização de Documentos...");

                    using var scope = _serviceProvider.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    // Dispara o comando em background
                    await mediator.Send(new SincronizarDocumentosCommand(), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha crítica durante a execução do Worker de Sincronização.");
                }

                _logger.LogInformation("Worker dormindo por 30 minutos.");

                // Otimização: Roda a cada 30 minutos (1800000 milissegundos)
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}