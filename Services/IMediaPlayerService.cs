using GerenciadorAulas.Models;
using System.Threading.Tasks;
using LibVLCSharp.Shared; // Adicionado para o tipo MediaPlayer

namespace GerenciadorAulas.Services
{
    public interface IMediaPlayerService
    {
        MediaPlayer MediaPlayer { get; } // Nova propriedade
        Task PlayAsync(VideoItem video);
        void Stop();
        bool IsPlaying { get; }
        int Volume { get; set; } // Nova propriedade
        void PlayPause(); // Novo método
        void ToggleMute(); // Novo método
        void VolumeUp(); // Novo método
        void VolumeDown(); // Novo método

        event EventHandler IsPlayingChanged; // Novo evento para notificar mudanças no estado de reprodução

        Task InitializeAsync(); // Novo método assíncrono para inicialização
    }
}
