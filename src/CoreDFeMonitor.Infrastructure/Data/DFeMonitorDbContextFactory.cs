// src/CoreDFeMonitor.Infrastructure/Data/DFeMonitorDbContextFactory.cs
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreDFeMonitor.Infrastructure.Data
{
    // O EF Core (via linha de comando) vai procurar automaticamente qualquer classe 
    // que implemente IDesignTimeDbContextFactory e usá-la para gerar as Migrations.
    public class DFeMonitorDbContextFactory : IDesignTimeDbContextFactory<DFeMonitorDbContext>
    {
        public DFeMonitorDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DFeMonitorDbContext>();

            // Replicamos a exata mesma lógica do DependencyInjection.cs para ele achar a pasta correta
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Join(path, "CoreDFeMonitor.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath};Cache=Shared;");

            return new DFeMonitorDbContext(optionsBuilder.Options);
        }
    }
}