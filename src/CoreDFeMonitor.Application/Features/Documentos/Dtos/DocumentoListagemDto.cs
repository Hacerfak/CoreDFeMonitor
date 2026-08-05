using System;

namespace CoreDFeMonitor.Application.Features.Documentos.Dtos
{
    public record DocumentoListagemDto(
        Guid Id,
        string Nsu,
        string Numero,
        string ChaveAcesso,
        string SchemaDisplay,
        string CnpjCpf,
        string Emitente,
        string ValorTotal,
        string SituacaoSefaz,
        string StatusManifestacao,
        DateTimeOffset DataEmissao,
        bool CienciaEnviada,
        string XmlConteudo
    );
}