using System.Collections.Generic;
using System.Collections.ObjectModel;
using GerenciadorAulas.Models;

namespace GerenciadorAulas.Services
{
    public interface ITreeViewDataService
    {
        ObservableCollection<object> TreeRoot { get; }
        HashSet<string> ItensCarregados { get; }
        HashSet<string> VideosAssistidos { get; set; }

        void LoadInitialTree();
        Task AddFolderOrVideo(string? path);
        void RemoveFolder(FolderItem folder);
        IEnumerable<VideoItem> GetAllVideosRecursive(object item);
        VideoItem? GetNextUnwatchedVideo(FolderItem? startFolder = null);
        IEnumerable<VideoItem> GetVideosRecursive(FolderItem folder);
        void SaveTreeViewEstado();
        void CarregarEstadoTreeView();
        void CarregarEstadoVideosAssistidos();
        void SalvarEstadoVideosAssistidos();
        void AtualizarCheckboxFolder(FolderItem folder);
        void AtualizarPais(FolderItem? folder);
        (int total, int marked) ContarVideos(object item);
        void AtualizarNomeComProgresso(FolderItem folder);
    }
}
