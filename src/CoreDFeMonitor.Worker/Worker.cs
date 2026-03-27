// src/CoreDFeMonitor.Worker/Worker.cs
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Services;
using Microsoft.Extensions.Hosting;

namespace CoreDFeMonitor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ZeusBackgroundService _zeusBackgroundService;

        public Worker(ZeusBackgroundService zeusBackgroundService)
        {
            _zeusBackgroundService = zeusBackgroundService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // O próprio serviço delega todo o poder para o Motor Híbrido que construímos na Application!
            await _zeusBackgroundService.IniciarAsync(stoppingToken);
        }
    }
}