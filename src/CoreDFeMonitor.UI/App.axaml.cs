// src/CoreDFeMonitor.UI/App.axaml.cs
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoreDFeMonitor.Application;
using CoreDFeMonitor.Infrastructure;
using CoreDFeMonitor.Infrastructure.Data;
using CoreDFeMonitor.UI.ViewModels;
using CoreDFeMonitor.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Avalonia.Platform.Storage;

namespace CoreDFeMonitor.UI
{
    public partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var services = new ServiceCollection();

                // Infraestrutura e Aplicação
                services.AddLogging(builder => { builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information); });
                services.AddInfrastructure();
                services.AddApplication();

                // Registro dos ViewModels (Eles precisam ser Singleton ou Transient dependendo do caso)
                // O MainViewModel deve ser Singleton para manter o estado da tela
                services.AddSingleton<MainViewModel>();
                services.AddTransient<CadastroEmpresaViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ConfiguracoesViewModel>();

                var serviceProvider = services.BuildServiceProvider();

                // Cria o banco de dados se não existir
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DFeMonitorDbContext>();
                    dbContext.Database.EnsureCreated();
                }

                // Resgata o Roteador Principal
                var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // Injeta serviços especiais se necessário (como o StorageProvider para abrir certificados)
                // Isto é opcional se não usar mais injeção direta da janela, mas mantemos para compatibilidade
                // services.AddSingleton<Avalonia.Platform.Storage.IStorageProvider>(mainWindow.StorageProvider);

                desktop.MainWindow = mainWindow;

                // MAGIA: Inicializa o roteamento (Busca na DB e define a tela)
                _ = mainViewModel.InicializarAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}