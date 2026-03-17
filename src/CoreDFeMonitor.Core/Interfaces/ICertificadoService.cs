// src/CoreDFeMonitor.Core/Interfaces/ICertificadoService.cs
using System.Security.Cryptography.X509Certificates;

namespace CoreDFeMonitor.Core.Interfaces
{
    public interface ICertificadoService
    {
        X509Certificate2 ObterCertificadoDoArquivo(string caminho, string senha);
        X509Certificate2? ObterCertificadoPeloNumeroSerie(string numeroSerie);
    }
}