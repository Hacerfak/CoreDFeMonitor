// src/CoreDFeMonitor.Core/Entities/Empresa.cs
using System;

namespace CoreDFeMonitor.Core.Entities
{
    public class Empresa
    {
        public Guid Id { get; private set; }
        public string Cnpj { get; private set; }
        public string RazaoSocial { get; private set; }
        public string Uf { get; private set; } // Nova propriedade para o Estado (Ex: "SP", "RS")
        public string? CaminhoCertificado { get; private set; }
        public string? SenhaCertificado { get; private set; }
        public DateTime DataCadastro { get; private set; }

        protected Empresa() { }

        public Empresa(string cnpj, string razaoSocial, string uf)
        {
            Id = Guid.NewGuid();
            Cnpj = ValidarEFormatarCnpj(cnpj);
            RazaoSocial = razaoSocial;
            Uf = uf.ToUpper(); // Garante que ficará maiúsculo
            DataCadastro = DateTime.UtcNow;

            if (Uf.Length != 2)
                throw new ArgumentException("A UF deve conter exatamente 2 caracteres (Ex: SP, MG).");
        }

        public void AtualizarDados(string razaoSocial, string uf)
        {
            RazaoSocial = razaoSocial;
            Uf = uf.ToUpper();
        }

        public void ConfigurarCertificado(string caminhoCertificado, string senha)
        {
            CaminhoCertificado = caminhoCertificado;
            SenhaCertificado = senha;
        }

        private string ValidarEFormatarCnpj(string cnpj)
        {
            var cnpjLimpo = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpjLimpo.Length != 14)
                throw new ArgumentException("CNPJ inválido. O CNPJ deve conter 14 dígitos.");

            return cnpjLimpo;
        }
    }
}