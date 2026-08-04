// Caminho: src/CoreDFeMonitor.Application/Features/Documentos/Commands/SincronizarDocumentosCommand.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;
using CoreDFeMonitor.Application.Services;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Application.Features.Documentos.Commands
{
    public class SincronizarDocumentosCommand : IRequest<bool> { }

    public class SincronizarDocumentosCommandHandler : IRequestHandler<SincronizarDocumentosCommand, bool>
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IEmitenteRepository _emitenteRepository;
        private readonly ISefazService _sefazService;
        private readonly ISyncStatusMonitor _syncStatus;
        private readonly IArmazenamentoXmlService _armazenamentoXmlService;
        private readonly INotificacaoDesktopService _notificacao;
        private readonly ILogger<SincronizarDocumentosCommandHandler> _logger;

        // TRAVA DE CONCORRÊNCIA
        private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public SincronizarDocumentosCommandHandler(
            IEmpresaRepository empresaRepository,
            IDocumentoRepository documentoRepository,
            IEmitenteRepository emitenteRepository,
            ISefazService sefazService,
            ISyncStatusMonitor syncStatus,
            IArmazenamentoXmlService armazenamentoXmlService,
            INotificacaoDesktopService notificacao,
            ILogger<SincronizarDocumentosCommandHandler> logger)
        {
            _empresaRepository = empresaRepository;
            _documentoRepository = documentoRepository;
            _emitenteRepository = emitenteRepository;
            _sefazService = sefazService;
            _syncStatus = syncStatus;
            _armazenamentoXmlService = armazenamentoXmlService;
            _notificacao = notificacao;
            _logger = logger;
        }

        public async Task<bool> Handle(SincronizarDocumentosCommand request, CancellationToken cancellationToken)
        {
            if (!_syncLock.Wait(0))
            {
                _logger.LogWarning("Uma sincronização já está em andamento. Ignorando requisição concorrente.");
                return false;
            }

            try
            {
                int totalDocumentosNovosBaixados = 0;
                var empresas = await _empresaRepository.ObterTodasAsync(cancellationToken);

                foreach (var empresa in empresas)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    _syncStatus.AtualizarMensagem($"Sincronizando NF-e: {empresa.RazaoSocial}...");
                    _logger.LogInformation(">> Sincronizando empresa: {Razao} (NSU: {NSU})", empresa.RazaoSocial, empresa.UltimoNsu);

                    // ==============================================================
                    // AUTO-RECUPERAÇÃO DE CIÊNCIAS PENDENTES
                    // ==============================================================
                    var todosDocumentos = await _documentoRepository.ObterTodasAsync(cancellationToken);
                    var pendentes = todosDocumentos
                        .Where(d => d.EmpresaId == empresa.Id &&
                                    d.Schema.Contains("resNFe") &&
                                    !d.CienciaEnviada &&
                                    (DateTimeOffset.Now - d.DataEmissao).TotalDays <= 10)
                        .ToList();

                    if (pendentes.Any())
                    {
                        _logger.LogInformation(">> Tentando recuperar Ciência para {Count} resumos pendentes válidos...", pendentes.Count);
                        foreach (var doc in pendentes)
                        {
                            var ciencia = await _sefazService.EnviarCienciaOperacaoAsync(empresa, doc.ChaveAcesso);
                            if (ciencia.Sucesso)
                            {
                                doc.MarcarCienciaEnviada();
                                await _documentoRepository.AtualizarAsync(doc, cancellationToken);
                            }
                            await Task.Delay(2000, cancellationToken);
                        }
                    }

                    // ==============================================================
                    // DISTRIBUIÇÃO DFE
                    // ==============================================================
                    var resultado = await _sefazService.BaixarDocumentosAsync(empresa);

                    if (resultado.Sucesso)
                    {
                        var novosDocumentos = new List<Documento>();

                        foreach (var docZip in resultado.Documentos)
                        {
                            bool jaExiste = await _documentoRepository.ExisteNsuAsync(empresa.Id, docZip.Nsu, cancellationToken);

                            if (!jaExiste)
                            {
                                string cnpjEmitente = ExtrairCnpjCpf(docZip.XmlDescompactado);
                                string nomeEmitente = ExtrairNomeEmitente(docZip.XmlDescompactado);
                                string chaveAcesso = ExtrairChaveAcesso(docZip.XmlDescompactado);
                                DateTimeOffset dataEmissao = ExtrairDataEmissao(docZip.XmlDescompactado);

                                if (string.IsNullOrEmpty(cnpjEmitente))
                                    cnpjEmitente = "00000000000000";

                                var emitente = await _emitenteRepository.ObterPorCnpjAsync(cnpjEmitente, cancellationToken);

                                if (emitente == null)
                                {
                                    emitente = new Emitente
                                    {
                                        Cnpj = cnpjEmitente,
                                        RazaoSocial = nomeEmitente
                                    };

                                    await _emitenteRepository.AdicionarAsync(emitente, cancellationToken);
                                }

                                var novoDoc = new Documento(empresa.Id, docZip.Nsu, docZip.Schema, docZip.XmlDescompactado)
                                {
                                    EmitenteId = emitente.Id,
                                    ChaveAcesso = chaveAcesso, // CORREÇÃO: Agora a chave não é mais vazia
                                    DataEmissao = dataEmissao  // CORREÇÃO: Agora a data é extraída
                                };

                                if (novoDoc.RequerCienciaAutomatica(empresa.Cnpj))
                                {
                                    var cienciaResult = await _sefazService.EnviarCienciaOperacaoAsync(empresa, novoDoc.ChaveAcesso);
                                    if (cienciaResult.Sucesso) novoDoc.MarcarCienciaEnviada();
                                }

                                novosDocumentos.Add(novoDoc);

                                // Se não conseguiu extrair a chave, cria um fallback no nome para não sobreescrever
                                string nomeArquivoChave = string.IsNullOrEmpty(novoDoc.ChaveAcesso) ? Guid.NewGuid().ToString() : novoDoc.ChaveAcesso;

                                _ = _armazenamentoXmlService.SalvarXmlAsync(empresa.Cnpj, nomeArquivoChave, novoDoc.Schema, novoDoc.XmlConteudo);
                            }
                        }

                        if (novosDocumentos.Count > 0)
                        {
                            await _documentoRepository.AdicionarLoteAsync(novosDocumentos, cancellationToken);
                            totalDocumentosNovosBaixados += novosDocumentos.Count;
                        }

                        if (empresa.UltimoNsu != resultado.UltimoNsuRetornado)
                        {
                            empresa.AtualizarNsu(resultado.UltimoNsuRetornado);
                            await _empresaRepository.AtualizarAsync(empresa, cancellationToken);
                        }
                    }

                    await Task.Delay(5000, cancellationToken);
                }

                if (totalDocumentosNovosBaixados > 0)
                {
                    _notificacao.Exibir("Core DF-e Monitor", $"Sincronização concluída! {totalDocumentosNovosBaixados} novos documentos.");
                }

                return true;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        // =========================================================
        // MÉTODOS EXTRATORES DE XML
        // =========================================================
        private string ExtrairCnpjCpf(string xml)
        {
            var match = Regex.Match(xml, @"<(?:CNPJ|CPF)>([0-9]{11,14})</(?:CNPJ|CPF)>");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtrairNomeEmitente(string xml)
        {
            var match = Regex.Match(xml, @"<xNome>(.*?)</xNome>");
            return match.Success ? match.Groups[1].Value : "EMITENTE DESCONHECIDO";
        }

        private string ExtrairChaveAcesso(string xml)
        {
            // Tenta achar a tag chNFe (comum em resNFe e resEvento)
            var matchChNFe = Regex.Match(xml, @"<chNFe>([0-9]{44})</chNFe>");
            if (matchChNFe.Success) return matchChNFe.Groups[1].Value;

            // Tenta achar no ID da tag infNFe (comum no XML completo da NFe)
            var matchInfNFe = Regex.Match(xml, @"<infNFe[^>]*Id=""NFe([0-9]{44})""");
            if (matchInfNFe.Success) return matchInfNFe.Groups[1].Value;

            return string.Empty;
        }

        private DateTimeOffset ExtrairDataEmissao(string xml)
        {
            // Tenta dhEmi (Emissão da NFe/Resumo)
            var matchDhEmi = Regex.Match(xml, @"<dhEmi>(.*?)</dhEmi>");
            if (matchDhEmi.Success && DateTimeOffset.TryParse(matchDhEmi.Groups[1].Value, out var dhEmi))
                return dhEmi;

            // Tenta dhEvento (Data do Evento)
            var matchDhEvento = Regex.Match(xml, @"<dhEvento>(.*?)</dhEvento>");
            if (matchDhEvento.Success && DateTimeOffset.TryParse(matchDhEvento.Groups[1].Value, out var dhEvento))
                return dhEvento;

            // Tenta dhRecbto (Data de Recebimento na Sefaz como fallback)
            var matchDhRecbto = Regex.Match(xml, @"<dhRecbto>(.*?)</dhRecbto>");
            if (matchDhRecbto.Success && DateTimeOffset.TryParse(matchDhRecbto.Groups[1].Value, out var dhRecbto))
                return dhRecbto;

            // Se não achar nada, salva a hora do download
            return DateTimeOffset.Now;
        }
    }
}