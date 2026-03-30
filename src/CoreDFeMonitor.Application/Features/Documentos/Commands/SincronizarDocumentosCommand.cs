// src/CoreDFeMonitor.Application/Features/Documentos/Commands/SincronizarDocumentosCommand.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreDFeMonitor.Core.Entities;
using CoreDFeMonitor.Core.Interfaces;
using CoreDFeMonitor.Core.Mediator;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Application.Features.Documentos.Commands
{
    public class SincronizarDocumentosCommand : IRequest<bool> { }

    public class SincronizarDocumentosCommandHandler : IRequestHandler<SincronizarDocumentosCommand, bool>
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly ISefazService _sefazService;
        private readonly IArmazenamentoXmlService _armazenamentoXmlService; // NOVO
        private readonly ILogger<SincronizarDocumentosCommandHandler> _logger;

        // TRAVA DE CONCORRÊNCIA: Garante que apenas 1 sincronização rode por vez em todo o sistema.
        private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public SincronizarDocumentosCommandHandler(
            IEmpresaRepository empresaRepository,
            IDocumentoRepository documentoRepository,
            ISefazService sefazService,
            IArmazenamentoXmlService armazenamentoXmlService, // NOVO
            ILogger<SincronizarDocumentosCommandHandler> logger)
        {
            _empresaRepository = empresaRepository;
            _documentoRepository = documentoRepository;
            _sefazService = sefazService;
            _armazenamentoXmlService = armazenamentoXmlService; // NOVO
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
                var empresas = await _empresaRepository.ObterTodasAsync(cancellationToken);

                foreach (var empresa in empresas)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    _logger.LogInformation(">> Sincronizando empresa: {Razao} (NSU: {NSU})", empresa.RazaoSocial, empresa.UltimoNsu);

                    // ==============================================================
                    // NOVO: AUTO-RECUPERAÇÃO DE CIÊNCIAS PENDENTES (Self-Healing)
                    // ==============================================================
                    var todosDocumentos = await _documentoRepository.ObterTodasAsync(cancellationToken);
                    var pendentes = todosDocumentos
                        .Where(d => d.EmpresaId == empresa.Id &&
                                    d.Schema.Contains("resNFe") &&
                                    !d.CienciaEnviada &&
                                    (DateTimeOffset.Now - d.DataEmissao).TotalDays <= 10) // <--- O FILTRO AQUI
                        .ToList();

                    if (pendentes.Any())
                    {
                        _logger.LogInformation(">> Tentando recuperar Ciência para {Count} resumos pendentes válidos (menos de 10 dias)...", pendentes.Count);
                        foreach (var doc in pendentes)
                        {
                            var ciencia = await _sefazService.EnviarCienciaOperacaoAsync(empresa, doc.ChaveAcesso);
                            if (ciencia.Sucesso)
                            {
                                doc.MarcarCienciaEnviada();
                                await _documentoRepository.AtualizarAsync(doc, cancellationToken);
                            }
                            await Task.Delay(2000, cancellationToken); // Respiro de 2 segundos para a Sefaz
                        }
                    }
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
                                var novoDoc = new Documento(empresa.Id, docZip.Nsu, docZip.Schema, docZip.XmlDescompactado);

                                if (novoDoc.RequerCienciaAutomatica(empresa.Cnpj))
                                {
                                    var cienciaResult = await _sefazService.EnviarCienciaOperacaoAsync(empresa, novoDoc.ChaveAcesso);
                                    if (cienciaResult.Sucesso) novoDoc.MarcarCienciaEnviada();
                                }

                                novosDocumentos.Add(novoDoc);

                                // GRAVAR NO DISCO
                                if (novoDoc.ChaveAcesso != "SEM_CHAVE_NO_RESUMO")
                                {
                                    // Executa sem aguardar para não travar o fluxo da Sefaz
                                    _ = _armazenamentoXmlService.SalvarXmlAsync(empresa.Cnpj, novoDoc.ChaveAcesso, novoDoc.Schema, novoDoc.XmlConteudo);
                                }
                            }
                        }

                        if (novosDocumentos.Count > 0)
                        {
                            await _documentoRepository.AdicionarLoteAsync(novosDocumentos, cancellationToken);
                        }

                        if (empresa.UltimoNsu != resultado.UltimoNsuRetornado)
                        {
                            empresa.AtualizarNsu(resultado.UltimoNsuRetornado);
                            await _empresaRepository.AtualizarAsync(empresa, cancellationToken);
                        }
                    }

                    // === NOVO: BUSCA DE CT-E ===
                    _logger.LogInformation(">> Sincronizando CT-es da empresa: {Razao} (NSU: {NSU})", empresa.RazaoSocial, empresa.UltimoNsuCte);

                    var resultadoCte = await _sefazService.BaixarDocumentosCteAsync(empresa);

                    if (resultadoCte.Sucesso)
                    {
                        var novosCtes = new List<Documento>();

                        foreach (var docZip in resultadoCte.Documentos)
                        {
                            // Para CT-e, podemos adicionar um prefixo no NSU no BD para não dar conflito com o da NF-e, 
                            // ou usar o Schema para diferenciar. Mas o banco permite porque o Schema será diferente (procCTe).
                            // Vamos adicionar "CTE_" na frente do NSU só para o index único do banco não falhar caso os NSUs coincidam
                            string nsuUnico = "CTE_" + docZip.Nsu;

                            bool jaExiste = await _documentoRepository.ExisteNsuAsync(empresa.Id, nsuUnico, cancellationToken);
                            if (!jaExiste)
                            {
                                var novoDoc = new Documento(empresa.Id, nsuUnico, docZip.Schema, docZip.XmlDescompactado);

                                // CT-e não possui Evento de "Ciência da Emissão" obrigatório para liberação do XML,
                                // o WebService do CT-e já entrega o XML completo (procCTe) de cara!
                                // Portanto, apenas salvamos no banco e no disco!

                                novosCtes.Add(novoDoc);

                                if (novoDoc.ChaveAcesso != "SEM_CHAVE_NO_RESUMO")
                                {
                                    _ = _armazenamentoXmlService.SalvarXmlAsync(empresa.Cnpj, novoDoc.ChaveAcesso, novoDoc.Schema, novoDoc.XmlConteudo);
                                }
                            }
                        }

                        if (novosCtes.Count > 0)
                            await _documentoRepository.AdicionarLoteAsync(novosCtes, cancellationToken);

                        if (empresa.UltimoNsuCte != resultadoCte.UltimoNsuRetornado)
                        {
                            empresa.AtualizarNsuCte(resultadoCte.UltimoNsuRetornado);
                            await _empresaRepository.AtualizarAsync(empresa, cancellationToken);
                        }
                    }

                    // Intervalo de segurança da SEFAZ
                    await Task.Delay(5000, cancellationToken);
                }

                return true;
            }
            finally
            {
                _syncLock.Release();
            }
        }
    }
}