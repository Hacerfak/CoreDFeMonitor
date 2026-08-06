using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Application.Features.Documentos.Dtos;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;

namespace CoreDFeMonitor.Application.Features.Documentos.Queries
{
    // A classe ObterDocumentosQuery continua igual aqui em cima...
    public class ObterDocumentosQuery : IRequest<List<DocumentoListagemDto>>
    {
        public Guid? EmpresaId { get; set; }
        public int? EmitenteId { get; set; }
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

            if (request.EmpresaId.HasValue && request.EmpresaId.Value != Guid.Empty)
                query = query.Where(d => d.EmpresaId == request.EmpresaId.Value);

            if (request.EmitenteId.HasValue && request.EmitenteId.Value > 0)
                query = query.Where(d => d.EmitenteId == request.EmitenteId.Value);

            if (request.DataInicio.HasValue)
                query = query.Where(d => d.DataEmissao.Date >= request.DataInicio.Value.Date);

            if (request.DataFim.HasValue)
                query = query.Where(d => d.DataEmissao.Date <= request.DataFim.Value.Date);

            if (request.TipoDocumento != "Todos")
                query = query.Where(d => d.TipoDocumento.Equals(request.TipoDocumento, StringComparison.OrdinalIgnoreCase));

            var listaFinal = new List<DocumentoListagemDto>();

            foreach (var doc in query)
            {
                string cnpj = doc.Emitente?.Cnpj ?? "-";
                string emitente = doc.Emitente?.RazaoSocial ?? "Emitente Desconhecido";
                string valor = ExtrairTag(doc.XmlConteudo, "vNF", "0.00") ?? "0.00";

                string numero = ExtrairNumero(doc.ChaveAcesso, doc.XmlConteudo);
                string situacao = ExtrairSituacaoSefaz(doc.XmlConteudo, doc.TipoDocumento);

                // NOVO: Extrai a manifestação priorizando o banco de dados
                string statusManifestacao = ExtrairManifestacao(doc);

                // REGRAS DE TELA
                bool isNfe = doc.TipoDocumento.Equals("NFe", StringComparison.OrdinalIgnoreCase);

                // Pode manifestar se for NFe e ainda não tiver manifestação conclusiva (null ou apenas Ciência)
                bool podeManifestar = isNfe && (!doc.CodigoManifestacao.HasValue || doc.CodigoManifestacao == 210210);

                if (!string.IsNullOrWhiteSpace(request.FiltroTexto))
                {
                    if (!doc.ChaveAcesso.Contains(request.FiltroTexto) &&
                        !cnpj.Contains(request.FiltroTexto) &&
                        !emitente.Contains(request.FiltroTexto, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                listaFinal.Add(new DocumentoListagemDto(
                    doc.Id, doc.Nsu, numero, doc.ChaveAcesso, doc.TipoDocumento,
                    cnpj, emitente, $"R$ {valor}", situacao, statusManifestacao,
                    doc.DataEmissao, doc.CienciaEnviada, doc.XmlConteudo,
                    podeManifestar, isNfe
                ));
            }

            return listaFinal.OrderByDescending(x => x.DataEmissao).ToList();
        }

        private string? ExtrairTag(string xml, string tag, string? padrao)
        {
            var match = Regex.Match(xml, $"<{tag}>(.*?)</{tag}>");
            return match.Success ? match.Groups[1].Value : padrao;
        }

        private string ExtrairNumero(string chave, string xml)
        {
            var nNF = ExtrairTag(xml, "nNF", null);
            if (!string.IsNullOrEmpty(nNF)) return nNF;

            if (!string.IsNullOrEmpty(chave) && chave.Length == 44)
                return chave.Substring(25, 9).TrimStart('0');

            return "-";
        }

        private string ExtrairSituacaoSefaz(string xml, string tipo)
        {
            if (tipo == "Resumo" || tipo == "NFe")
            {
                var cSitNFe = ExtrairTag(xml, "cSitNFe", "1");
                return cSitNFe switch
                {
                    "1" => "Autorizada",
                    "2" => "Denegada",
                    "3" => "Cancelada",
                    _ => "Desconhecida"
                };
            }
            return "Vinculado";
        }

        private string ExtrairManifestacao(Documento doc)
        {
            if (doc.TipoDocumento == "Evento") return doc.NomeEvento;

            // 1º Prioridade: O que gravamos no nosso banco de dados (ação nossa)
            if (doc.CodigoManifestacao.HasValue)
            {
                return doc.CodigoManifestacao.Value switch
                {
                    210200 => "Confirmada",
                    210240 => "Não Realizada",
                    210220 => "Desconhecida",
                    210210 => "Ciência",
                    _ => "Sem Manifestação"
                };
            }

            // 2º Prioridade: O que veio no XML da SEFAZ
            var cSitConf = ExtrairTag(doc.XmlConteudo, "cSitConf", "0");
            return cSitConf switch
            {
                "1" => "Confirmada",
                "2" => "Desconhecida",
                "3" => "Não Realizada",
                "4" => "Ciência",
                _ => "Sem Manifestação"
            };
        }
    }
}