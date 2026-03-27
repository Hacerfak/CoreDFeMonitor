// src/CoreDFeMonitor.Core/Entities/Empresa.cs
using System;

namespace CoreDFeMonitor.Core.Entities
{
    public class Empresa
    {
        public Guid Id { get; private set; }
        public string Cnpj { get; private set; } = string.Empty;
        public string RazaoSocial { get; private set; } = string.Empty;
        public string Uf { get; private set; } = string.Empty;
        public string? InscricaoEstadual { get; private set; }
        public string? Telefone { get; private set; }
        public string? Email { get; private set; }

        // Dados de Endereço detalhados
        public string? Logradouro { get; private set; }
        public string? Numero { get; private set; }
        public string? Complemento { get; private set; }
        public string? Bairro { get; private set; }
        public long? CodigoMunicipio { get; private set; } // Código IBGE
        public string? NomeMunicipio { get; private set; }
        public string? Cep { get; private set; }

        public string? CaminhoCertificado { get; private set; }
        public string? SenhaCertificado { get; private set; }
        public DateTime DataCadastro { get; private set; }
        public string UltimoNsu { get; private set; } = "000000000000000"; // Sempre 15 dígitos
        public string UltimoNsuCte { get; private set; } = "000000000000000";

        protected Empresa() { }

        public Empresa(string cnpj, string razaoSocial, string uf, string? inscricaoEstadual,
                       string? logradouro, string? numero, string? complemento, string? bairro,
                       long? codigoMunicipio, string? nomeMunicipio, string? cep,
                       string? telefone, string? email)
        {
            Id = Guid.NewGuid();
            Cnpj = ValidarEFormatarCnpj(cnpj);
            RazaoSocial = razaoSocial;
            Uf = uf.ToUpper();
            InscricaoEstadual = inscricaoEstadual;

            Logradouro = logradouro;
            Numero = numero;
            Complemento = complemento;
            Bairro = bairro;
            CodigoMunicipio = codigoMunicipio;
            NomeMunicipio = nomeMunicipio;
            Cep = cep;

            Telefone = telefone;
            Email = email;
            DataCadastro = DateTime.UtcNow;

            if (Uf.Length != 2)
                throw new ArgumentException("A UF deve conter exatamente 2 caracteres (Ex: SP, MG).");
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

        public void AtualizarNsu(string novoNsu)
        {
            if (!string.IsNullOrEmpty(novoNsu))
                UltimoNsu = novoNsu.PadLeft(15, '0');
        }

        public void AtualizarNsuCte(string novoNsu)
        {
            if (!string.IsNullOrEmpty(novoNsu))
                UltimoNsuCte = novoNsu.PadLeft(15, '0');
        }
    }
}