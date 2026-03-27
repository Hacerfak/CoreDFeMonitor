// src/CoreDFeMonitor.Infrastructure/Data/DFeMonitorDbContext.cs
using CoreDFeMonitor.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreDFeMonitor.Infrastructure.Data
{
      public class DFeMonitorDbContext : DbContext
      {
            public DbSet<Empresa> Empresas { get; set; } = null!;
            public DbSet<Documento> Documentos { get; set; } = null!;

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
                        entity.Property(e => e.InscricaoEstadual).HasMaxLength(20);
                        entity.Property(e => e.Logradouro).HasMaxLength(200);
                        entity.Property(e => e.Numero).HasMaxLength(20);
                        entity.Property(e => e.Complemento).HasMaxLength(100);
                        entity.Property(e => e.Bairro).HasMaxLength(100);
                        entity.Property(e => e.NomeMunicipio).HasMaxLength(150);
                        entity.Property(e => e.Cep).HasMaxLength(10);
                        entity.Property(e => e.Telefone).HasMaxLength(20);
                        entity.Property(e => e.Email).HasMaxLength(150);
                        entity.Property(e => e.UltimoNsu).IsRequired().HasMaxLength(15).HasDefaultValue("000000000000000");
                        entity.Property(e => e.UltimoNsuCte).IsRequired().HasMaxLength(15).HasDefaultValue("000000000000000");
                        entity.Property(e => e.DataCadastro);
                  });

                  modelBuilder.Entity<Documento>(entity =>
                  {
                        entity.ToTable("Documentos");
                        entity.HasKey(d => d.Id);
                        entity.Property(d => d.Nsu).HasMaxLength(15);
                        entity.Property(d => d.ChaveAcesso).HasMaxLength(44);
                        entity.Property(d => d.CienciaEnviada).IsRequired();
                        entity.Property(d => d.TipoDocumento).HasMaxLength(30);
                        entity.Property(d => d.TipoEvento).HasMaxLength(10);
                        entity.Property(d => d.NomeEvento).HasMaxLength(100);
                        entity.HasIndex(d => new { d.EmpresaId, d.Nsu }).IsUnique(); // Impede baixar o mesmo NSU 2x
                  });
            }
      }
}