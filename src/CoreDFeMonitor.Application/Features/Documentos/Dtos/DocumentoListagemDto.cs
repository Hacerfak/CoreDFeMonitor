namespace CoreDFeMonitor.Application.Features.Documentos.Dtos
{
    public record DocumentoListagemDto(
        Guid Id,
        string Nsu,
        string ChaveAcesso,
        string SchemaDisplay,
        string CnpjCpf,
        string Emitente,
        string ValorTotal,
        string SituacaoSefaz,
        DateTimeOffset DataEmissao,
        DateTimeOffset DataImportacao,
        bool CienciaEnviada,
        string XmlConteudo
    );
}