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

        long Length { get; } // Duração total do vídeo em milissegundos
        long Time { get; set; } // Posição atual do vídeo em milissegundos
        float Position { get; set; } // Posição atual do vídeo (0.0f a 1.0f)

        event EventHandler IsPlayingChanged; // Novo evento para notificar mudanças no estado de reprodução

        Task InitializeAsync(); // Novo método assíncrono para inicialização

        // Métodos para Playlist
        Task SetPlaylistAndPlayAsync(IEnumerable<VideoItem> playlist, bool startFromBeginning = true);
        void PlayNext();
        void PlayPrevious();
        bool HasNext { get; }
        bool HasPrevious { get; }

        event EventHandler<VideoItem> VideoEnded; // Novo evento para notificar quando um vídeo termina
    }
}
