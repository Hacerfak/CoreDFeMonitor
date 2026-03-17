// src/CoreDFeMonitor.UI/App.axaml.cs
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoreDFeMonitor.Application;
using CoreDFeMonitor.Infrastructure;
using CoreDFeMonitor.Infrastructure.Data;
using CoreDFeMonitor.UI.ViewModels;
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
                // 1. Instanciamos a janela primeiro para pegar a referência nativa do S.O.
                var mainWindow = new MainWindow();

                // 2. Configuração do Container de Injeção de Dependências
                var services = new ServiceCollection();

                services.AddLogging();        // Adiciona Logging

                services.AddInfrastructure(); // Adiciona EF Core SQLite e Zeus Fiscal
                services.AddApplication();    // Adiciona o MediatR

                // Registra o StorageProvider nativo da janela (para selecionar o certificado .pfx)
                services.AddSingleton<IStorageProvider>(mainWindow.StorageProvider);

                // Registra o ViewModel
                services.AddTransient<CadastroEmpresaViewModel>();

                var serviceProvider = services.BuildServiceProvider();

                // 3. Garante que o banco de dados SQLite físico seja criado na máquina
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DFeMonitorDbContext>();
                    dbContext.Database.EnsureCreated();
                }

                // 4. Liga o DataContext (ViewModel) à Janela
                mainWindow.DataContext = serviceProvider.GetRequiredService<CadastroEmpresaViewModel>();
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}