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

        // NOVO: Propriedade para filtrar pelo clique no painel lateral
        public int? EmitenteId { get; set; }
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

            // 1. FILTRO DE EMITENTE
            if (request.EmitenteId.HasValue && request.EmitenteId.Value > 0)
                query = query.Where(d => d.EmitenteId == request.EmitenteId.Value);

            // 2. FILTRO DE DATAS
            if (request.DataInicio.HasValue)
                query = query.Where(d => d.DataEmissao.Date >= request.DataInicio.Value.Date);

            if (request.DataFim.HasValue)
                query = query.Where(d => d.DataEmissao.Date <= request.DataFim.Value.Date);

            // 3. FILTRO DE TIPO (Removido CT-e)
            if (request.TipoDocumento == "NF-e")
                query = query.Where(d => d.Schema.Contains("nfe", StringComparison.OrdinalIgnoreCase));
            else if (request.TipoDocumento == "Eventos")
                query = query.Where(d => d.Schema.Contains("evento", StringComparison.OrdinalIgnoreCase));

            var listaFinal = new List<DocumentoListagemDto>();

            foreach (var doc in query)
            {
                // Usando os dados diretamente da tabela relacionada Emitente
                string cnpj = doc.Emitente?.Cnpj ?? "-";
                string emitente = doc.Emitente?.RazaoSocial ?? "Emitente Desconhecido";

                string valor = ExtrairTag(doc.XmlConteudo, "vNF", "0.00") ?? "0.00";

                string schemaDisplay = MapearSchema(doc.Schema);
                if (doc.TipoDocumento.StartsWith("Evento") && !string.IsNullOrEmpty(doc.NomeEvento))
                    schemaDisplay = doc.NomeEvento;

                string situacao = ExtrairSituacaoSefaz(doc.XmlConteudo, schemaDisplay);

                if (!string.IsNullOrWhiteSpace(request.FiltroTexto))
                {
                    if (!doc.ChaveAcesso.Contains(request.FiltroTexto) &&
                        !cnpj.Contains(request.FiltroTexto) &&
                        !emitente.Contains(request.FiltroTexto, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                listaFinal.Add(new DocumentoListagemDto(
                    doc.Id, doc.Nsu, doc.ChaveAcesso, schemaDisplay,
                    cnpj, emitente, $"R$ {valor}", situacao,
                    doc.DataEmissao, doc.DataProcessamento, doc.CienciaEnviada, doc.XmlConteudo
                ));
            }

            return listaFinal.OrderByDescending(x => x.DataEmissao).ToList();
        }

        private string? ExtrairTag(string xml, string tag, string? padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string ExtrairSituacaoSefaz(string xml, string tipoDocumento)
        {
            if (tipoDocumento.Contains("Resumo") || tipoDocumento.Contains("NF-e"))
            {
                var match = Regex.Match(xml, @"<cSitNFe>([0-9])</cSitNFe>");
                if (match.Success)
                {
                    return match.Groups[1].Value switch
                    {
                        "1" => "Autorizada",
                        "2" => "Denegada",
                        "3" => "Cancelada",
                        _ => "Desconhecida"
                    };
                }
                return "Autorizada";
            }
            return "Vinculado";
        }

        private string MapearSchema(string schema)
        {
            if (schema.Contains("procNFe")) return "NF-e Completa";
            if (schema.Contains("resNFe")) return "Resumo NF-e";
            if (schema.Contains("resEvento") || schema.Contains("procEvento") || schema.Contains("retEnvEvento")) return "Evento Sefaz";
            return "Outro";
        }
    }
}