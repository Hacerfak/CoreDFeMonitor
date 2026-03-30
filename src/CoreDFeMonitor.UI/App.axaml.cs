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
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


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

                services.AddLogging(builder => { builder.AddConsole(); builder.SetMinimumLevel(LogLevel.Information); });
                services.AddInfrastructure();
                services.AddApplication();

                services.AddSingleton<MainViewModel>();
                services.AddTransient<CadastroEmpresaViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ConfiguracoesViewModel>();
                services.AddTransient<DocumentosViewModel>(); // A tela que criamos na etapa anterior!

                var serviceProvider = services.BuildServiceProvider();

                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DFeMonitorDbContext>();
                    dbContext.Database.Migrate();
                }

                var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

                var mainWindow = new MainWindow { DataContext = mainViewModel };
                desktop.MainWindow = mainWindow;

                // Inicializa as telas
                _ = mainViewModel.InicializarAsync();

                // ==============================================================
                // LIGA O MOTOR DO WORKER EM BACKGROUND (Fogo na máquina!)
                // ==============================================================
                var worker = serviceProvider.GetRequiredService<CoreDFeMonitor.Application.Services.ZeusBackgroundService>();
                _ = Task.Run(() => worker.IniciarAsync()); // Roda numa thread solta, sem travar a UI!
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}