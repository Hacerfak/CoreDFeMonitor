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

            // 1. FILTRO DE DATAS - AGORA USANDO A DATA DE EMISSÃO!
            if (request.DataInicio.HasValue)
                query = query.Where(d => d.DataEmissao.Date >= request.DataInicio.Value.Date);
            if (request.DataFim.HasValue)
                query = query.Where(d => d.DataEmissao.Date <= request.DataFim.Value.Date);

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
                // Novo Extrator de CNPJ e Emitente robusto
                string cnpj = ExtrairCnpjCpf(doc.XmlConteudo);
                string emitente = ExtrairEmitente(doc.XmlConteudo);
                string valor = ExtrairTag(doc.XmlConteudo, "vNF", null) ?? ExtrairTag(doc.XmlConteudo, "vTPrest", "0.00") ?? "0.00";

                string schemaDisplay = doc.TipoDocumento;
                if (doc.TipoDocumento.StartsWith("Evento") && !string.IsNullOrEmpty(doc.NomeEvento))
                    schemaDisplay = doc.NomeEvento;

                string situacao = ExtrairSituacaoSefaz(doc.XmlConteudo, doc.TipoDocumento);

                // Aplica Filtro Texto (agora filtrando também por CNPJ)
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

            return listaFinal.OrderByDescending(x => x.Nsu).ToList();
        }

        // =========================================================
        // MÉTODOS EXTRATORES MELHORADOS
        // =========================================================
        private string? ExtrairTag(string xml, string tag, string? padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string ExtrairEmitente(string xml)
        {
            // Pega o primeiro <xNome> que encontrar (Funciona tanto para Resumo quanto procNFe)
            var match = Regex.Match(xml, @"<xNome>(.*?)</xNome>");
            return match.Success ? match.Groups[1].Value : "Emitente Desconhecido";
        }

        private string ExtrairCnpjCpf(string xml)
        {
            var match = Regex.Match(xml, @"<(?:CNPJ|CPF)>([0-9]{11,14})</(?:CNPJ|CPF)>");
            if (match.Success)
            {
                string doc = match.Groups[1].Value;
                return doc.Length == 14 ?
                    Convert.ToUInt64(doc).ToString(@"00\.000\.000\/0000\-00") :
                    Convert.ToUInt64(doc).ToString(@"000\.000\.000\-00");
            }
            return "-";
        }

        private string ExtrairSituacaoSefaz(string xml, string tipoDocumento)
        {
            if (tipoDocumento.Contains("Resumo") || tipoDocumento == "NF-e")
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
                return "Autorizada"; // procNFe só baixa se autorizada
            }
            return "Vinculado"; // Para eventos
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