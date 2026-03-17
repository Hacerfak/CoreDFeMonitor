// src/CoreDFeMonitor.Infrastructure/Services/CertificadoService.cs
using System.Security.Cryptography.X509Certificates;
using CoreDFeMonitor.Core.Interfaces;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class CertificadoService : ICertificadoService
    {
        public X509Certificate2 ObterCertificadoDoArquivo(string caminho, string senha)
        {
            // Uso correto e seguro para .NET 9 e .NET 10 via X509CertificateLoader
            // O uso do MachineKeySet e PersistKeySet evita problemas de permissão em serviços de background no Windows/Linux
            return X509CertificateLoader.LoadPkcs12FromFile(caminho, senha,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);
        }

        public X509Certificate2? ObterCertificadoPeloNumeroSerie(string numeroSerie)
        {
            // A busca no repositório do S.O. não sofreu alteração, pois o certificado já foi carregado e validado pelo sistema.
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certCollection = store.Certificates.Find(X509FindType.FindBySerialNumber, numeroSerie, true);
            return certCollection.Count > 0 ? certCollection[0] : null;
        }
    }
}