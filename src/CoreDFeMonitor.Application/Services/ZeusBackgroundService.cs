// src/CoreDFeMonitor.Application/Services/ZeusBackgroundService.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Features.Documentos.Commands;
using CoreDFeMonitor.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Application.Services
{
    public class ZeusBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ZeusBackgroundService> _logger;

        public ZeusBackgroundService(IServiceProvider serviceProvider, ILogger<ZeusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task IniciarAsync(CancellationToken stoppingToken = default)
        {
            // O arquivo de Lock ficará na mesma pasta do Banco de Dados
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lockPath = Path.Join(path, "CoreDFeMonitor_Worker.lock");

            _logger.LogInformation("⚡ [MOTOR HÍBRIDO] Verificando concorrência de processos...");

            while (!stoppingToken.IsCancellationRequested)
            {
                FileStream? lockFile = null;
                try
                {
                    // TENTA PEGAR O CONTROLE EXCLUSIVO
                    // Se outro processo (UI ou Serviço) já estiver com o arquivo aberto, o OS lança IOException.
                    lockFile = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                    _logger.LogInformation("✅ [MOTOR HÍBRIDO] Controle exclusivo adquirido! Este processo assumiu as buscas em background.");

                    // Loop de processamento de quem detém o controle
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                                await mediator.Send(new SincronizarDocumentosCommand(), stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro no loop de sincronização: {Message}", ex.Message);
                        }

                        // Pausa de 30 minutos (Padrão Sefaz para Background)
                        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                    }
                }
                catch (IOException)
                {
                    // ARQUIVO TRANCADO = Outro executável já está fazendo o trabalho!
                    _logger.LogInformation("⏳ [MOTOR HÍBRIDO] Outro serviço de background detectado rodando. Este processo operará de forma passiva.");
                }
                finally
                {
                    // Se o programa for fechado, ele solta o arquivo para o outro processo assumir
                    lockFile?.Dispose();
                }

                // Se não conseguiu pegar o controle (ou perdeu por algum motivo), 
                // dorme 1 minuto e tenta novamente. Isso garante o FAILOVER AUTOMÁTICO!
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}