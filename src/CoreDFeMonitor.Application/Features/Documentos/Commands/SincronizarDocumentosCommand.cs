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
            if (!_syncLock.Wait(0)) return false;

            try
            {
                int totalDocumentosNovosBaixados = 0;
                var empresas = await _empresaRepository.ObterTodasAsync(cancellationToken);

                foreach (var empresa in empresas)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // ==============================================================
                    // 1. VERIFICAÇÃO INTELIGENTE DE PROTEÇÃO (ANTI-CONSUMO INDEVIDO)
                    // ==============================================================
                    if (empresa.EstaEmEsperaObrigatoriaSefaz())
                    {
                        string horaLiberacao = empresa.LiberacaoSefazEm()?.ToString("HH:mm") ?? "--:--";
                        _syncStatus.AtualizarMensagem($"[Proteção] {empresa.RazaoSocial} em espera (Livre às {horaLiberacao}).");

                        await Task.Delay(2000, cancellationToken); // Pausa visual para a UI
                        continue; // Pula a empresa sem bater na SEFAZ
                    }

                    _syncStatus.AtualizarMensagem($"Processando: {empresa.RazaoSocial}...");

                    // ==============================================================
                    // 2. AUTO-RECUPERAÇÃO DE CIÊNCIAS PENDENTES
                    // ==============================================================
                    var pendentes = await _documentoRepository.ObterPendentesDeCienciaAsync(empresa.Id, cancellationToken);

                    if (pendentes.Any())
                    {
                        _logger.LogInformation(">> Enviando Ciência para {Count} resumos...", pendentes.Count);
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
                    // 3. DISTRIBUIÇÃO DFE (Lotes Inteligentes)
                    // ==============================================================
                    bool continuarBuscando = true;
                    int lotesBaixados = 0;

                    while (continuarBuscando)
                    {
                        lotesBaixados++;
                        _syncStatus.AtualizarMensagem($"Sincronizando NF-e Sefaz (Lote {lotesBaixados})...");

                        var resultado = await _sefazService.BaixarDocumentosAsync(empresa);

                        if (!resultado.Sucesso)
                            break;

                        var novosDocumentos = new List<Documento>();

                        foreach (var docZip in resultado.Documentos)
                        {
                            bool jaExiste = await _documentoRepository.ExisteNsuAsync(empresa.Id, docZip.Nsu, cancellationToken);

                            if (!jaExiste)
                            {
                                string xml = docZip.XmlDescompactado;
                                string schemaLower = docZip.Schema.ToLower();

                                string chaveAcesso = ExtrairChaveAcesso(xml);
                                string cnpjEmitente = ExtrairCnpjCpf(xml);
                                string nomeEmitente = ExtrairNomeEmitente(xml);
                                DateTimeOffset dataEmissao = ExtrairDataEmissao(xml);

                                string tipoDoc = "NFe";
                                string tipoEv = string.Empty;
                                string nomeEv = string.Empty;

                                if (schemaLower.Contains("resnfe"))
                                {
                                    tipoDoc = "Resumo";
                                }
                                else if (schemaLower.Contains("evento"))
                                {
                                    tipoDoc = "Evento";
                                    tipoEv = ExtrairTipoEvento(xml);
                                    nomeEv = ExtrairNomeEvento(xml);
                                }

                                if (string.IsNullOrEmpty(chaveAcesso))
                                    chaveAcesso = $"SEM_CHAVE_{docZip.Nsu}_{Guid.NewGuid().ToString().Substring(0, 5)}";

                                if (string.IsNullOrEmpty(cnpjEmitente))
                                    cnpjEmitente = "00000000000000";

                                var emitente = await _emitenteRepository.ObterPorCnpjAsync(cnpjEmitente, cancellationToken);
                                if (emitente == null)
                                {
                                    emitente = new Emitente { Cnpj = cnpjEmitente, RazaoSocial = nomeEmitente };
                                    await _emitenteRepository.AdicionarAsync(emitente, cancellationToken);
                                }

                                var novoDoc = new Documento(empresa.Id, docZip.Nsu, docZip.Schema, docZip.XmlDescompactado)
                                {
                                    EmitenteId = emitente.Id,
                                    ChaveAcesso = chaveAcesso,
                                    DataEmissao = dataEmissao,
                                    TipoDocumento = tipoDoc,
                                    TipoEvento = tipoEv,
                                    NomeEvento = nomeEv
                                };

                                if (novoDoc.RequerCienciaAutomatica(empresa.Cnpj) && !novoDoc.ChaveAcesso.StartsWith("SEM_CHAVE"))
                                {
                                    var cienciaResult = await _sefazService.EnviarCienciaOperacaoAsync(empresa, novoDoc.ChaveAcesso);
                                    if (cienciaResult.Sucesso) novoDoc.MarcarCienciaEnviada();
                                }

                                novosDocumentos.Add(novoDoc);
                                _ = _armazenamentoXmlService.SalvarXmlAsync(empresa.Cnpj, novoDoc.ChaveAcesso, novoDoc.Schema, novoDoc.XmlConteudo);
                            }
                        }

                        if (novosDocumentos.Count > 0)
                        {
                            await _documentoRepository.AdicionarLoteAsync(novosDocumentos, cancellationToken);
                            totalDocumentosNovosBaixados += novosDocumentos.Count;
                        }

                        // Atualiza as flags de proteção e NSU da Empresa
                        bool precisaAtualizarEmpresa = false;

                        if (empresa.UltimoNsu != resultado.UltimoNsuRetornado)
                        {
                            empresa.AtualizarNsu(resultado.UltimoNsuRetornado);
                            precisaAtualizarEmpresa = true;
                        }

                        if (resultado.Documentos.Count == 0)
                        {
                            // A Sefaz não devolveu nada (cStat 137). Ativa a trava de 1 hora.
                            empresa.RegistrarConsultaVazia();
                            precisaAtualizarEmpresa = true;
                            continuarBuscando = false;
                        }
                        else
                        {
                            // A fila andou! Limpamos qualquer bloqueio anterior e damos fôlego para o próximo lote.
                            empresa.LimparBloqueioConsulta();
                            precisaAtualizarEmpresa = true;
                            await Task.Delay(2000, cancellationToken);
                        }

                        if (precisaAtualizarEmpresa)
                        {
                            await _empresaRepository.AtualizarAsync(empresa, cancellationToken);
                        }
                    }
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
            var matchChNFe = Regex.Match(xml, @"<chNFe>([0-9]{44})</chNFe>");
            if (matchChNFe.Success) return matchChNFe.Groups[1].Value;

            var matchInfNFe = Regex.Match(xml, @"<infNFe[^>]*Id=""NFe([0-9]{44})""");
            if (matchInfNFe.Success) return matchInfNFe.Groups[1].Value;

            return string.Empty;
        }

        private DateTimeOffset ExtrairDataEmissao(string xml)
        {
            var matchDhEmi = Regex.Match(xml, @"<(?:dhEmi|dEmi)>(.*?)</(?:dhEmi|dEmi)>");
            if (matchDhEmi.Success && DateTimeOffset.TryParse(matchDhEmi.Groups[1].Value, out var dhEmi))
                return dhEmi;

            var matchDhEvento = Regex.Match(xml, @"<dhEvento>(.*?)</dhEvento>");
            if (matchDhEvento.Success && DateTimeOffset.TryParse(matchDhEvento.Groups[1].Value, out var dhEvento))
                return dhEvento;

            var matchDhRecbto = Regex.Match(xml, @"<dhRecbto>(.*?)</dhRecbto>");
            if (matchDhRecbto.Success && DateTimeOffset.TryParse(matchDhRecbto.Groups[1].Value, out var dhRecbto))
                return dhRecbto;

            return DateTimeOffset.Now;
        }

        private string ExtrairTipoEvento(string xml)
        {
            var match = Regex.Match(xml, @"<tpEvento>(.*?)</tpEvento>");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtrairNomeEvento(string xml)
        {
            var match = Regex.Match(xml, @"<(?:xEvento|descEvento)>(.*?)</(?:xEvento|descEvento)>");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}