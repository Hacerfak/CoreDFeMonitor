// src/CoreDFeMonitor.Application/Features/Documentos/Dtos/DocumentoListagemDto.cs
using System;

namespace CoreDFeMonitor.Application.Features.Documentos.Dtos
{
    public record DocumentoListagemDto(
        Guid Id,
        string Nsu,
        string ChaveAcesso,
        string SchemaDisplay,
        string Emitente,
        string ValorTotal,
        DateTime DataEmissao,
        bool CienciaEnviada,
        string XmlConteudo
    );
}