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