// src/CoreDFeMonitor.Infrastructure/DependencyInjection.cs
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Infrastructure.Data;
using CoreDFeMonitor.Infrastructure.Data.Repositories;
using CoreDFeMonitor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreDFeMonitor.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Define o local do ficheiro SQLite (Pasta LocalApplicationData do utilizador)
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            path = Path.Join(path, "CoreDFeMonitor");
            Directory.CreateDirectory(path);

            var dbPath = Path.Join(path, "CoreDFeMonitor.db");

            // Regista o DbContext com o SQLite
            services.AddDbContext<DFeMonitorDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            Console.WriteLine($"\n[INFRAESTRUTURA] Banco de Dados SQLite configurado em: {dbPath}\n");

            // Repositórios
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            services.AddScoped<IDocumentoRepository, DocumentoRepository>();

            // Serviços Sefaz / Zeus
            services.AddSingleton<ICertificadoService, CertificadoService>(); // Pode ser Singleton pois não tem estado
            services.AddScoped<ISefazService, SefazService>(); // Scoped pois fará integrações transacionais

            return services;
        }
    }
}