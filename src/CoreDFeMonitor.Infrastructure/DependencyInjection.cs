// src/CoreDFeMonitor.Infrastructure/DependencyInjection.cs
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Infrastructure.Data;
using CoreDFeMonitor.Infrastructure.Data.Repositories;
using CoreDFeMonitor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;

namespace CoreDFeMonitor.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Define o local do ficheiro SQLite (Pasta LocalApplicationData do utilizador)
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Join(path, "CoreDFeMonitor.db");

            // Regista o DbContext com o SQLite
            services.AddDbContext<DFeMonitorDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Repositórios
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();

            // Serviços Sefaz / Zeus
            services.AddSingleton<ICertificadoService, CertificadoService>(); // Pode ser Singleton pois não tem estado
            services.AddScoped<ISefazService, SefazService>(); // Scoped pois fará integrações transacionais

            return services;
        }
    }
}