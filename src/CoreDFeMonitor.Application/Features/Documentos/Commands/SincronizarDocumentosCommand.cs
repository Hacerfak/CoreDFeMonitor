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
        private readonly ILogger<SincronizarDocumentosCommandHandler> _logger;

        public SincronizarDocumentosCommandHandler(
            IEmpresaRepository empresaRepository,
            IDocumentoRepository documentoRepository,
            ISefazService sefazService,
            ILogger<SincronizarDocumentosCommandHandler> logger)
        {
            _empresaRepository = empresaRepository;
            _documentoRepository = documentoRepository;
            _sefazService = sefazService;
            _logger = logger;
        }

        public async Task<bool> Handle(SincronizarDocumentosCommand request, CancellationToken cancellationToken)
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

                            // ==============================================================
                            // MAGIA ACONTECENDO AQUI: Ciência Automática
                            // ==============================================================
                            if (novoDoc.RequerCienciaAutomatica(empresa.Cnpj))
                            {
                                _logger.LogInformation("Documento (Chave: {Chave}) emitido contra o CNPJ detectado. Enviando Ciência (210210)...", novoDoc.ChaveAcesso);

                                var cienciaResult = await _sefazService.EnviarCienciaOperacaoAsync(empresa, novoDoc.ChaveAcesso);

                                if (cienciaResult.Sucesso)
                                {
                                    novoDoc.MarcarCienciaEnviada();
                                    _logger.LogInformation("Ciência registrada na Sefaz com sucesso: {Msg}", cienciaResult.Mensagem);
                                }
                                else
                                {
                                    _logger.LogWarning("Sefaz negou o envio de Ciência: {Msg}", cienciaResult.Mensagem);
                                }
                            }

                            novosDocumentos.Add(novoDoc);
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

                // MOC Exigência: 5 segundos de intervalo para evitar cStat 656
                await Task.Delay(5000, cancellationToken);
            }

            return true;
        }
    }
}