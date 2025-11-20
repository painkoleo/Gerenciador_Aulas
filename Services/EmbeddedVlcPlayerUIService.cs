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

        private List<VideoItem> _playlist = new List<VideoItem>();
        private int _currentPlaylistIndex = -1;

        private int _previousVolume; // Armazena o volume antes de mutar

        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

        public event EventHandler? IsPlayingChanged; // Implementação do evento
        public event EventHandler<VideoItem>? VideoEnded; // Implementação do evento VideoEnded

        public MediaPlayer MediaPlayer => _mediaPlayer!;

        public long Length => _mediaPlayer?.Length ?? 0;

        public long Time
        {
            get => _mediaPlayer?.Time ?? 0;
            set
            {
                if (_mediaPlayer != null && _mediaPlayer.IsSeekable && _mediaPlayer.Time != value)
                {
                    _mediaPlayer.Time = value;
                }
            }
        }

        public float Position
        {
            get => _mediaPlayer?.Position ?? 0.0f;
            set
            {
                if (_mediaPlayer != null && _mediaPlayer.IsSeekable && _mediaPlayer.Position != value)
                {
                    _mediaPlayer.Position = value;
                }
            }
        }

        private int _currentVolume = 50; // Valor padrão

        public int Volume
        {
            get => _currentVolume;
            set
            {
                if (_currentVolume != value)
                {
                    _currentVolume = value;
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Volume = value;
                    }
                }
            }
        }

        public void ToggleMute()
        {
            if (_currentVolume > 0)
            {
                _previousVolume = _currentVolume;
                Volume = 0; // Define o volume para 0, o set da propriedade cuidará do _mediaPlayer
            }
            else
            {
                Volume = _previousVolume > 0 ? _previousVolume : 50; // Restaura o volume anterior ou um padrão
            }
        }

        public void VolumeUp()
        {
            Volume = Math.Min(100, _currentVolume + 5);
        }

        public void VolumeDown()
        {
            Volume = Math.Max(0, _currentVolume - 5);
        }

        private readonly ITreeViewDataService _treeViewDataService;

        public EmbeddedVlcPlayerUIService(ITreeViewDataService treeViewDataService)
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: Construtor chamado.");
            _treeViewDataService = treeViewDataService;
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
                _mediaPlayer.EndReached += async (sender, e) => { // Adicionar manipulador de EndReached aqui
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: MediaPlayer_EndReached evento disparado.");
                    LogService.Log($"[DEBUG] EmbeddedVlcPlayerUIService: MediaPlayer.Time: {_mediaPlayer?.Time}, MediaPlayer.Length: {_mediaPlayer?.Length}");
                    
                    // Disparar o evento VideoEnded antes de tentar o próximo vídeo
                    if (_currentPlaylistIndex >= 0 && _currentPlaylistIndex < _playlist.Count)
                    {
                        var currentVideoInPlaylist = _playlist[_currentPlaylistIndex];
                        // Obter a instância "oficial" do VideoItem da TreeRoot
                        var officialVideoItem = _treeViewDataService.GetVideoItemByPath(currentVideoInPlaylist.FullPath);
                        if (officialVideoItem != null)
                        {
                            VideoEnded?.Invoke(this, officialVideoItem);
                        }
                        else
                        {
                            LogService.LogWarning($"[DEBUG] EmbeddedVlcPlayerUIService: Não foi possível encontrar o VideoItem oficial para {currentVideoInPlaylist.FullPath} na TreeRoot.");
                            // Se não encontrar o oficial, ainda podemos disparar com o da playlist, mas o check na UI pode não funcionar.
                            VideoEnded?.Invoke(this, currentVideoInPlaylist);
                        }
                    }

                    _playbackCompletionCts?.Cancel(); // Cancela a espera em PlayAsync
                    LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _playbackCompletionCts.Cancel() chamado por EndReached.");
                    
                    // Tentar reproduzir o próximo vídeo na playlist de forma assíncrona
                    await Task.Run(() => PlayNext());
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



        public bool HasNext => _playlist.Any() && _currentPlaylistIndex < _playlist.Count - 1;
        public bool HasPrevious => _playlist.Any() && _currentPlaylistIndex > 0;

        public async Task SetPlaylistAndPlayAsync(IEnumerable<VideoItem> playlist, bool startFromBeginning = true)
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: SetPlaylistAndPlayAsync chamado.");
            _playlist.Clear();
            _playlist.AddRange(playlist);

            if (!_playlist.Any())
            {
                LogService.LogWarning("Playlist vazia. Nenhuma reprodução iniciada.");
                Stop();
                return;
            }

            _currentPlaylistIndex = startFromBeginning ? 0 : -1; // -1 para que PlayNext() comece do 0

            if (startFromBeginning)
            {
                await PlayCurrentVideo();
            }
            else
            {
                // Se não for para começar do início, apenas prepara a playlist
                // A reprodução será iniciada por um PlayNext() ou PlayAsync() externo
                Stop(); // Garante que não há nada tocando
            }
        }

        public void PlayNext()
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: PlayNext chamado.");
            if (HasNext)
            {
                _currentPlaylistIndex++;
                PlayCurrentVideo().ConfigureAwait(false);
            }
            else
            {
                LogService.Log("[DEBUG] Fim da playlist. Parando reprodução.");
                Stop();
            }
        }

        public void PlayPrevious()
        {
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: PlayPrevious chamado.");
            if (HasPrevious)
            {
                _currentPlaylistIndex--;
                PlayCurrentVideo().ConfigureAwait(false);
            }
            else
            {
                LogService.Log("[DEBUG] Início da playlist. Parando reprodução.");
                Stop();
            }
        }

        private async Task PlayCurrentVideo()
        {
            if (_mediaPlayer == null || _currentPlaylistIndex < 0 || _currentPlaylistIndex >= _playlist.Count)
            {
                LogService.LogWarning("Não há vídeo para reproduzir na posição atual da playlist.");
                Stop();
                return;
            }

            var video = _playlist[_currentPlaylistIndex];
            LogService.Log($"[DEBUG] EmbeddedVlcPlayerUIService: PlayCurrentVideo chamado para {video.FullPath}.");
            
            // Chamar o método PlayAsync interno
            await _PlayAsyncInternal(video);
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

        // Renomeado o PlayAsync original para um método interno
        private async Task _PlayAsyncInternal(VideoItem video)
        {
            LogService.Log($"[DEBUG] EmbeddedVlcPlayerUIService: _PlayAsyncInternal chamado para {video.FullPath}.");
            Debug.Assert(_mediaPlayer != null, "MediaPlayer não deveria ser nulo em _PlayAsyncInternal.");

            var media = new Media(_libVLC!, video.FullPath, FromType.FromPath);
            _mediaPlayer.Play(media);
            LogService.Log("[DEBUG] EmbeddedVlcPlayerUIService: _mediaPlayer.Play() chamado.");

            // Não aguardamos a conclusão do playback aqui.
            // A lógica de avanço para o próximo vídeo será tratada pelo evento EndReached.
            await Task.CompletedTask; // Retorna imediatamente
        }
    }
}
