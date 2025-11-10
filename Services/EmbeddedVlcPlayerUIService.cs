using System;
using System.Diagnostics; // Adicionado para Debug.Assert
using System.IO;
using System.Threading.Tasks;
using GerenciadorAulas.Models;
using GerenciadorAulas.ViewModels;
using LibVLCSharp.Shared;

namespace GerenciadorAulas.Services
{
    public class EmbeddedVlcPlayerUIService : IMediaPlayerService, IDisposable
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private CancellationTokenSource? _playbackCompletionCts;



        private int _previousVolume; // Armazena o volume antes de mutar

        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

        public event EventHandler? IsPlayingChanged; // Implementação do evento

        public MediaPlayer MediaPlayer => _mediaPlayer!;

        public int Volume
        {
            get => _mediaPlayer?.Volume ?? 0;
            set
            {
                if (_mediaPlayer != null && _mediaPlayer.Volume != value)
                {
                    _mediaPlayer.Volume = value;
                    _previousVolume = value; // Atualiza o volume anterior
                }
            }
        }

        public void ToggleMute()
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.Mute) // Usar a propriedade Mute
            {
                // Se estava mutado, desmutar e restaurar o volume anterior
                _mediaPlayer.Volume = _previousVolume;
                _mediaPlayer.Mute = false; // Definir Mute para false
            }
            else
            {
                // Se não estava mutado, armazenar o volume atual e mutar
                _previousVolume = _mediaPlayer.Volume;
                _mediaPlayer.Volume = 0;
                _mediaPlayer.Mute = true; // Definir Mute para true
            }
        }

        public void VolumeUp()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Min(100, _mediaPlayer.Volume + 5);
            }
        }

        public void VolumeDown()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = Math.Max(0, _mediaPlayer.Volume - 5);
            }
        }

        public EmbeddedVlcPlayerUIService() // Construtor sem parâmetros
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Construtor chamado.");
            // LibVLC e MediaPlayer serão inicializados em InitializeAsync
        }

        public Task InitializeAsync()
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: InitializeAsync chamado.");
            if (_libVLC == null)
            {
                LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Inicializando LibVLC e MediaPlayer.");
                _libVLC = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libVLC);
                _mediaPlayer.EndReached += (sender, e) => { // Adicionar manipulador de EndReached aqui
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: MediaPlayer_EndReached evento disparado.");
                    _playbackCompletionCts?.Cancel(); // Cancela a espera em PlayAsync
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _playbackCompletionCts.Cancel() chamado por EndReached.");
                };

                // Adicionar manipuladores de eventos para IsPlayingChanged
                _mediaPlayer.Playing += (sender, e) => {
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Evento Playing disparado.");
                    IsPlayingChanged?.Invoke(this, EventArgs.Empty);
                };
                _mediaPlayer.Paused += (sender, e) => {
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Evento Paused disparado.");
                    IsPlayingChanged?.Invoke(this, EventArgs.Empty);
                };
                _mediaPlayer.Stopped += (sender, e) => {
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Evento Stopped disparado.");
                    IsPlayingChanged?.Invoke(this, EventArgs.Empty);
                    // _playbackCompletion?.TrySetResult(true); // Removido
                    // LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _playbackCompletion.TrySetResult() chamado por Stopped.");
                };
            }
            return Task.CompletedTask;
        }

        public void PlayPause()
        {
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }
        }

        public void Stop() // Implementação do IMediaPlayerService.Stop()
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Stop() chamado.");
            if (_mediaPlayer != null)
            {
                LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Chamando _mediaPlayer.Stop().");
                _mediaPlayer.Stop();
            }
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Definindo _mediaPlayer.Media = null.");
            _mediaPlayer!.Media = null; // Limpa o Media atual
            _playbackCompletionCts?.Cancel(); // Cancela a espera em PlayAsync
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _playbackCompletionCts.Cancel() chamado por Stop().");        }

        public async Task PlayAsync(VideoItem video)
        {
            LogService.Log($"[DEBUG] EmbeddedVlcPlayerUIService: PlayAsync chamado para {video.FullPath}.");
            Debug.Assert(_mediaPlayer != null, "MediaPlayer não deveria ser nulo em PlayAsync.");

            var media = new Media(_libVLC!, video.FullPath, FromType.FromPath);
            _mediaPlayer.Play(media);
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _mediaPlayer.Play() chamado.");

            // Aguarda a conclusão do playback, que será setado por EndReached ou Stopped
            // Usar um CancellationTokenSource para controlar a espera
            var cts = new CancellationTokenSource();
            _playbackCompletionCts = cts; // Armazenar para que Stop() possa cancelar

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
                LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Playback cancelado.");
            }
            finally
            {
                _playbackCompletionCts = null;
            }
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Playback concluído.");
        }



        public void Dispose()
        {
            if (_mediaPlayer != null)
            {
                // _mediaPlayer.EndReached -= MediaPlayer_EndReached; // Removido
                _mediaPlayer.Dispose();
            }
            if (_libVLC != null)
            {
                _libVLC.Dispose();
            }
        }
    }
}
