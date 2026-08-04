using System.Collections.Generic;

namespace CoreDFeMonitor.Core.Entities
{
    public class Emitente
    {
        public int Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;

        public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
    }
}