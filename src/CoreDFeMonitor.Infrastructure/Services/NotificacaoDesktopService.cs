// src/CoreDFeMonitor.Infrastructure/Services/NotificacaoDesktopService.cs
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CoreDFeMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreDFeMonitor.Infrastructure.Services
{
    public class NotificacaoDesktopService : INotificacaoDesktopService
    {
        private readonly ILogger<NotificacaoDesktopService> _logger;

        public NotificacaoDesktopService(ILogger<NotificacaoDesktopService> logger)
        {
            _logger = logger;
        }

        public void Exibir(string titulo, string mensagem)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ExibirWindows(titulo, mensagem);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    ExibirLinux(titulo, mensagem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao disparar notificação na área de trabalho.");
            }
        }

        private void ExibirWindows(string titulo, string mensagem)
        {
            // Script PowerShell que invoca a API nativa de Toasts do Windows 10/11
            var script = $@"
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null;
            [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null;
            $xml = New-Object Windows.Data.Xml.Dom.XmlDocument;
            $xml.LoadXml('<toast><visual><binding template=""ToastText02""><text id=""1"">{titulo}</text><text id=""2"">{mensagem}</text></binding></visual></toast>');
            $toast = New-Object Windows.UI.Notifications.ToastNotification $xml;
            [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Core DF-e Monitor').Show($toast);";

            // Converter para Base64 garante que as aspas simples e duplas nunca quebrem o comando no terminal!
            var bytes = Encoding.Unicode.GetBytes(script);
            var base64 = Convert.ToBase64String(bytes);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand {base64}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }

        private void ExibirLinux(string titulo, string mensagem)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "notify-send",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                // A forma mais segura e à prova de falhas no .NET moderno!
                // O .NET cuida de encapsular os espaços e caracteres especiais nativamente para o Linux.
                startInfo.ArgumentList.Add(titulo);
                startInfo.ArgumentList.Add(mensagem);

                var process = new Process { StartInfo = startInfo };
                process.Start();

                string erro = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(erro))
                {
                    _logger.LogWarning("Aviso ao exibir notificação no Linux: {Erro}", erro);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao tentar usar o notify-send. Verifique se o pacote 'libnotify-bin' está instalado no Debian.");
            }
        }
    }
}