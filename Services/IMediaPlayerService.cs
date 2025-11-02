using GerenciadorAulas.Models;
using System.Threading.Tasks;

namespace GerenciadorAulas.Services
{
    public interface IMediaPlayerService
    {
        Task PlayAsync(VideoItem video);
        void Stop();
        bool IsPlaying { get; }
    }
}
