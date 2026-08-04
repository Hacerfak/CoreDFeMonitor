using CoreDFeMonitor.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data
{
      public class DFeMonitorDbContext : DbContext
      {
            public DbSet<Empresa> Empresas { get; set; } = null!;
            public DbSet<Emitente> Emitentes { get; set; } = null!;
            public DbSet<Documento> Documentos { get; set; } = null!;

            public DFeMonitorDbContext(DbContextOptions<DFeMonitorDbContext> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  base.OnModelCreating(modelBuilder);

                  modelBuilder.Entity<Empresa>(entity =>
                  {
                        entity.ToTable("Empresas");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Cnpj).IsRequired().HasMaxLength(14);
                        entity.HasIndex(e => e.Cnpj).IsUnique();
                        entity.Property(e => e.UltimoNsu).IsRequired().HasMaxLength(15).HasDefaultValue("000000000000000");
                        // Removido UltimoNsuCte
                  });

                  modelBuilder.Entity<Emitente>(entity =>
                  {
                        entity.ToTable("Emitentes");
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.Cnpj).IsRequired().HasMaxLength(14);
                        entity.Property(e => e.RazaoSocial).IsRequired().HasMaxLength(200);
                  });

                  modelBuilder.Entity<Documento>(entity =>
                  {
                        entity.ToTable("Documentos");
                        entity.HasKey(d => d.Id);
                        entity.Property(d => d.Nsu).HasMaxLength(15);
                        entity.Property(d => d.ChaveAcesso).HasMaxLength(44);

                        // Impede baixar o mesmo NSU 2x para a mesma Empresa
                        entity.HasIndex(d => new { d.EmpresaId, d.Nsu }).IsUnique();

                        // Relacionamento com o Emitente
                        entity.HasOne(d => d.Emitente)
                        .WithMany(e => e.Documentos)
                        .HasForeignKey(d => d.EmitenteId)
                        .OnDelete(DeleteBehavior.Restrict);
                  });
            }
      }
}