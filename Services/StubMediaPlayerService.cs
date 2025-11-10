using GerenciadorAulas.Models;
using System.Threading.Tasks;
using LibVLCSharp.Shared; // Adicionado para o tipo MediaPlayer

namespace GerenciadorAulas.Services
{
    public class StubMediaPlayerService : IMediaPlayerService
    {
        public MediaPlayer MediaPlayer => null!; // Retorna null para o stub

        public bool IsPlaying => false;

#pragma warning disable CS0067 // O evento 'IsPlayingChanged' nunca é usado
        public event EventHandler? IsPlayingChanged; // Implementação do evento
#pragma warning restore CS0067 // O evento 'IsPlayingChanged' nunca é usado

        public long Length => 0; // Retorna 0 para o stub
        public long Time { get; set; } = 0; // Retorna 0 para o stub
        public float Position { get; set; } = 0.0f; // Retorna 0.0f para o stub

        private int _currentVolume = 50; // Valor padrão para o stub
        public int Volume
        {
            get => _currentVolume;
            set => _currentVolume = value;
        }

        public Task PlayAsync(VideoItem video)
        {
            return Task.CompletedTask;
        }

        public void Stop()
        {
            // Não faz nada para o stub
        }

        public void PlayPause()
        {
            // Não faz nada para o stub
        }

        public void ToggleMute()
        {
            // Não faz nada para o stub
        }

        public void VolumeUp()
        {
            // Não faz nada para o stub
        }

        public void VolumeDown()
        {
            // Não faz nada para o stub
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task SetPlaylistAndPlayAsync(IEnumerable<VideoItem> playlist, bool startFromBeginning = true)
        {
            return Task.CompletedTask;
        }

        public void PlayNext() { }
        public void PlayPrevious() { }
        public bool HasNext => false;
        public bool HasPrevious => false;

#pragma warning disable CS0067 // O evento 'VideoEnded' nunca é usado
        public event EventHandler<VideoItem>? VideoEnded;
#pragma warning restore CS0067 // O evento 'VideoEnded' nunca é usado
    }
}