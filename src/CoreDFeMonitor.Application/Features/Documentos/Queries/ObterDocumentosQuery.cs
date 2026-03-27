// src/CoreDFeMonitor.Application/Features/Documentos/Queries/ObterDocumentosQuery.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Queries
{
    public class ObterDocumentosQuery : IRequest<List<DocumentoListagemDto>>
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string FiltroTexto { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = "Todos";
    }

    public class ObterDocumentosQueryHandler : IRequestHandler<ObterDocumentosQuery, List<DocumentoListagemDto>>
    {
        private readonly IDocumentoRepository _documentoRepository;

        public ObterDocumentosQueryHandler(IDocumentoRepository documentoRepository)
        {
            _documentoRepository = documentoRepository;
        }

        public async Task<List<DocumentoListagemDto>> Handle(ObterDocumentosQuery request, CancellationToken cancellationToken)
        {
            var todos = await _documentoRepository.ObterTodasAsync(cancellationToken);
            var query = todos.AsEnumerable();

            if (request.DataInicio.HasValue)
                query = query.Where(d => d.DataProcessamento.Date >= request.DataInicio.Value.Date);
            if (request.DataFim.HasValue)
                query = query.Where(d => d.DataProcessamento.Date <= request.DataFim.Value.Date);

            // FILTROS ATUALIZADOS PARA SUPORTAR CT-E
            if (request.TipoDocumento == "NF-e")
                query = query.Where(d => d.Schema.Contains("nfe", StringComparison.OrdinalIgnoreCase));
            else if (request.TipoDocumento == "CT-e")
                query = query.Where(d => d.Schema.Contains("cte", StringComparison.OrdinalIgnoreCase));
            else if (request.TipoDocumento == "Eventos")
                query = query.Where(d => d.Schema.Contains("evento", StringComparison.OrdinalIgnoreCase));

            var listaFinal = new List<DocumentoListagemDto>();
            foreach (var doc in query)
            {
                // Extração inteligente de Emitente (busca especificamente dentro da tag <emit>)
                string emitente = ExtrairEmitente(doc.XmlConteudo);

                // Tenta achar o Valor da NF-e, se não achar, tenta o Valor do CT-e
                string valor = ExtrairTag(doc.XmlConteudo, "vNF", null) ?? ExtrairTag(doc.XmlConteudo, "vTPrest", "0.00") ?? "0.00";

                if (!string.IsNullOrWhiteSpace(request.FiltroTexto))
                {
                    if (!doc.ChaveAcesso.Contains(request.FiltroTexto) &&
                        !emitente.Contains(request.FiltroTexto, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                listaFinal.Add(new DocumentoListagemDto(
                    doc.Id, doc.Nsu, doc.ChaveAcesso,
                    MapearSchema(doc.Schema), emitente, $"R$ {valor}",
                    doc.DataProcessamento, doc.CienciaEnviada, doc.XmlConteudo
                ));
            }

            return listaFinal.OrderByDescending(x => x.Nsu).ToList();
        }

        private string? ExtrairTag(string xml, string tag, string? padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string ExtrairEmitente(string xml)
        {
            var match = Regex.Match(xml, @"<emit>.*?<xNome>(.*?)</xNome>.*?</emit>", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : "Emitente Desconhecido";
        }

        private string MapearSchema(string schema)
        {
            if (schema.Contains("procNFe")) return "NF-e Completa";
            if (schema.Contains("resNFe")) return "Resumo NF-e";

            // ADICIONADOS SCHEMAS DE CT-E
            if (schema.Contains("procCTe")) return "CT-e Completo";
            if (schema.Contains("resCTe")) return "Resumo CT-e";

            if (schema.Contains("resEvento") || schema.Contains("procEvento") || schema.Contains("retEnvEvento")) return "Evento Sefaz";
            return "Outro";
        }
    }
}