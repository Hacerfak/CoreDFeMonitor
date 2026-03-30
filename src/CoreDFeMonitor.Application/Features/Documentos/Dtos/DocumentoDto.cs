// src/CoreDFeMonitor.Application/Features/Documentos/Dtos/DocumentoDto.cs
using System;

namespace CoreDFeMonitor.Application.Features.Documentos.Dtos
{
    public record DocumentoDto(
        Guid Id,
        string Nsu,
        string ChaveAcesso,
        string SchemaDisplay, // Ex: NFe Proc, Resumo NFe, Cancelamento
        DateTimeOffset DataProcessamento,
        bool CienciaEnviada
    );
}