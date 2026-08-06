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
        string XmlConteudo,
        bool PodeManifestar, // Controla se o botão de manifestação fica habilitado
        bool IsNFe           // Controla se os botões de ação aparecem
    );
}