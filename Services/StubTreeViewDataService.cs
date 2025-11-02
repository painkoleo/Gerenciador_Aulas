using System.Collections.Generic;
using System.Collections.ObjectModel;
using GerenciadorAulas.Models;

namespace GerenciadorAulas.Services
{
    public class StubTreeViewDataService : ITreeViewDataService
    {
        public ObservableCollection<object> TreeRoot { get; } = new ObservableCollection<object>();
        public HashSet<string> ItensCarregados { get; } = new HashSet<string>();
        public HashSet<string> VideosAssistidos { get; set; } = new HashSet<string>();

        public void LoadInitialTree() { }
        public Task AddFolderOrVideo(string? path) { return Task.CompletedTask; }
        public void RemoveFolder(FolderItem folder) { }
        public IEnumerable<VideoItem> GetAllVideosRecursive(object item) { yield break; }
        public IEnumerable<VideoItem> GetVideosRecursive(FolderItem folder) { yield break; }
        public VideoItem? GetNextUnwatchedVideo(FolderItem? startFolder = null) { return null; }
        public void SaveTreeViewEstado() { }
        public void CarregarEstadoTreeView() { }
        public void CarregarEstadoVideosAssistidos() { }
        public void SalvarEstadoVideosAssistidos() { }
        public void AtualizarCheckboxFolder(FolderItem folder) { }
        public void AtualizarPais(FolderItem? folder) { }
        public void AtualizarNomeComProgresso(FolderItem folder) { }
        public (int total, int marked) ContarVideos(object item) { return (0, 0); }
    }
}
