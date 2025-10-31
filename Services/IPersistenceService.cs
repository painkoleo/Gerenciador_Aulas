using System.Collections.Generic;

namespace GerenciadorAulas.Services
{
    public interface IPersistenceService
    {
        HashSet<string> LoadWatchedVideos();
        void SaveWatchedVideos(HashSet<string> watchedVideos);
        TreeViewEstado LoadTreeViewEstado();
        void SaveTreeViewEstado(TreeViewEstado treeViewEstado);
        void SaveLastPlayedVideo(string videoPath);
    }

    public class StubPersistenceService : IPersistenceService
    {
        public HashSet<string> LoadWatchedVideos() => new HashSet<string>();
        public void SaveWatchedVideos(HashSet<string> watchedVideos) { }
        public TreeViewEstado LoadTreeViewEstado() => new TreeViewEstado();
        public void SaveTreeViewEstado(TreeViewEstado treeViewEstado) { }
        public void SaveLastPlayedVideo(string videoPath) { }
    }

    public class TreeViewEstado
    {
        public List<string> Pastas { get; set; } = new List<string>();
        public List<string> PastasExpandidas { get; set; } = new List<string>();
    }
}