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
        public string TipoDocumento { get; set; } = "Todos"; // Todos, NFe, Eventos
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

            // 1. Filtro de Datas (Usamos a DataProcessamento como base se dhEmi não existir)
            if (request.DataInicio.HasValue)
                query = query.Where(d => d.DataProcessamento.Date >= request.DataInicio.Value.Date);
            if (request.DataFim.HasValue)
                query = query.Where(d => d.DataProcessamento.Date <= request.DataFim.Value.Date);

            // 2. Filtro de Tipo
            if (request.TipoDocumento == "NF-e")
                query = query.Where(d => d.Schema.Contains("nfe", StringComparison.OrdinalIgnoreCase));
            else if (request.TipoDocumento == "Eventos")
                query = query.Where(d => d.Schema.Contains("evento", StringComparison.OrdinalIgnoreCase));

            // 3. Extração On-The-Fly (Em sistemas reais gigantes, essas colunas ficam no banco)
            var listaFinal = new List<DocumentoListagemDto>();
            foreach (var doc in query)
            {
                string emitente = ExtrairTag(doc.XmlConteudo, "xNome", "Emitente Desconhecido");
                string valor = ExtrairTag(doc.XmlConteudo, "vNF", "0.00");

                // 4. Filtro de Texto (Chave ou Emitente)
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

        private string ExtrairTag(string xml, string tag, string padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string MapearSchema(string schema)
        {
            if (schema.Contains("procNFe")) return "NF-e Completa";
            if (schema.Contains("resNFe")) return "Resumo NF-e";
            if (schema.Contains("resEvento")) return "Evento Sefaz";
            return "Outro";
        }
    }
}