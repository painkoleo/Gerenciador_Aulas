using GerenciadorAulas.Models;
using System.Threading.Tasks;

namespace GerenciadorAulas.Services
{
    public class StubMediaPlayerService : IMediaPlayerService
    {
        public bool IsPlaying => false;

        public Task PlayAsync(VideoItem video)
        {
            return Task.CompletedTask;
        }

        public void Stop()
        {
            // Do nothing
        }
    }
}
