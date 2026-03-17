// src/CoreDFeMonitor.Infrastructure/Data/DFeMonitorDbContext.cs
using CoreDFeMonitor.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data
{
    public class DFeMonitorDbContext : DbContext
    {
        public DbSet<Empresa> Empresas { get; set; } = null!;

        public DFeMonitorDbContext(DbContextOptions<DFeMonitorDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeamento da Entidade Empresa (Fluent API)
            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.ToTable("Empresas");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Cnpj)
                      .IsRequired()
                      .HasMaxLength(14);

                entity.HasIndex(e => e.Cnpj)
                      .IsUnique(); // O CNPJ deve ser único no sistema

                entity.Property(e => e.Uf)
                      .IsRequired()
                      .HasMaxLength(2);

                entity.Property(e => e.RazaoSocial)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.CaminhoCertificado)
                      .HasMaxLength(500);

                entity.Property(e => e.SenhaCertificado)
                      .HasMaxLength(200);
            });
        }
    }
}