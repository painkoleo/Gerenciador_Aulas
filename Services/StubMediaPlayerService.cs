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

        public int Volume { get; set; } = 0; // Retorna 0 para o stub

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
    }
}