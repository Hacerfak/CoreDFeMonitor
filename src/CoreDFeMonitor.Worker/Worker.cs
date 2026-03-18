// src/CoreDFeMonitor.Worker/Worker.cs
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
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço Baseado em Eventos do Zeus Fiscal iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Usa scope para não esgotar as memórias do banco no Singleton do Worker
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(new SincronizarDocumentosCommand(), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro de processamento no Worker: {Message}", ex.Message);
                }

                _logger.LogInformation("Ciclo finalizado. Pausa de 5 minutos antes da próxima varredura.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}