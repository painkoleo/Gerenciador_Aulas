using GerenciadorAulas.Exceptions;
using GerenciadorAulas.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GerenciadorAulas.Services
{
    public class MpvPlayerService : IMediaPlayerService
    {
        private readonly IWindowManager _windowManager;
        private readonly IPersistenceService _persistenceService;
        private readonly Func<Configuracoes> _configuracoesProvider;

        private CancellationTokenSource? _cts;
        private Process? _mpvProcess;
        private Task? _playbackTask;

        public bool IsPlaying => _mpvProcess != null && !_mpvProcess.HasExited;

        private static MpvPlayerService? _instance; // Campo estático para a instância única
        private static readonly object _lock = new object(); // Objeto para lock

        // Construtor privado para garantir que apenas uma instância seja criada
        private MpvPlayerService(IWindowManager windowManager, IPersistenceService persistenceService, Func<Configuracoes> configuracoesProvider)
        {
            _windowManager = windowManager;
            _persistenceService = persistenceService;
            _configuracoesProvider = configuracoesProvider;
        }

        // Método estático para obter a instância única
        public static MpvPlayerService GetInstance(IWindowManager windowManager, IPersistenceService persistenceService, Func<Configuracoes> configuracoesProvider)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new MpvPlayerService(windowManager, persistenceService, configuracoesProvider);
                }
                return _instance;
            }
        }

        public async Task PlayAsync(VideoItem video)
        {
            var configuracoes = _configuracoesProvider();
            if (!IsMpvPathValid(configuracoes.MPVPath))
            {
                throw new MpvPathNotConfiguredException("O caminho para o executável do MPV não foi configurado ou é inválido.");
            }

            Stop(); // Para qualquer reprodução anterior

            _cts = new CancellationTokenSource();
            try
            {
                _playbackTask = Task.Run(() => PlayInternal(video, configuracoes, _cts.Token), _cts.Token);
                await _playbackTask; // Aguarda a conclusão do Task
            }
            catch (OperationCanceledException)
            {
                LogService.Log("[MpvPlayerService] Reprodução de vídeo cancelada.");
            }
            catch (Exception ex)
            {
                LogService.LogError($"[MpvPlayerService] Erro durante a reprodução de vídeo: {ex.Message}", ex);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }

        }

        public void Stop()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }

            if (_mpvProcess != null && !_mpvProcess.HasExited)
            {
                try
                {
                    _mpvProcess.Kill();
                    _mpvProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    LogService.Log($"Erro ao finalizar MPV: {ex.Message}");
                }
                finally
                {
                    _mpvProcess?.Dispose();
                    _mpvProcess = null;
                }
            }

            _cts?.Dispose();
            _cts = null;
        }

        private void PlayInternal(VideoItem video, Configuracoes configuracoes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            LogService.Log($"[MpvPlayerService] Reproduzindo vídeo: {video.FullPath}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                video.IsChecked = true;
                _persistenceService.SaveLastPlayedVideo(video.FullPath);
            });

            try
            {
                string args = (configuracoes.MPVFullscreen ? "--fullscreen " : "") + $"\"{video.FullPath}\"";
                _mpvProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = configuracoes.MPVPath,
                    Arguments = args,
                    UseShellExecute = false
                });

                LogService.Log($"[MpvPlayerService] MPV iniciado para: {video.FullPath}. Aguardando término...");
                _mpvProcess?.WaitForExit();
                LogService.Log($"[MpvPlayerService] MPV para {video.FullPath} terminou. Exit Code: {_mpvProcess?.ExitCode}");
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                LogService.LogError($"[MpvPlayerService] Erro ao iniciar o MPV. Caminho: {configuracoes.MPVPath}", ex);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _windowManager.ShowMessageBox($"Erro ao iniciar o MPV: {ex.Message}. Verifique o caminho nas Configurações.");
                });
            }
            finally
            {
                _mpvProcess?.Dispose();
                _mpvProcess = null;
            }
        }

        private bool IsMpvPathValid(string mpvPath)
        {
            return !string.IsNullOrEmpty(mpvPath) && File.Exists(mpvPath);
        }
    }
}
