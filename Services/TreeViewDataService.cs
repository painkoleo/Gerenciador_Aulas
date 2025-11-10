using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using GerenciadorAulas.Models;

namespace GerenciadorAulas.Services
{
    public class TreeViewDataService : ITreeViewDataService
    {
        private readonly IWindowManager _windowManager;
        private readonly IPersistenceService _persistenceService;
        private readonly Func<Configuracoes> _configuracoesProvider;

        public ObservableCollection<object> TreeRoot { get; } = new ObservableCollection<object>();
        public HashSet<string> ItensCarregados { get; } = new HashSet<string>();
        public HashSet<string> VideosAssistidos { get; set; } = new HashSet<string>();

        private bool _isInitializing = false;

        public TreeViewDataService(IWindowManager windowManager, IPersistenceService persistenceService, Func<Configuracoes> configuracoesProvider)
        {
            _windowManager = windowManager;
            _persistenceService = persistenceService;
            _configuracoesProvider = configuracoesProvider;
        }

        public async Task LoadInitialTree()
        {
            CarregarEstadoVideosAssistidos();
            await CarregarEstadoTreeView();
        }

        public async Task AddFolderOrVideo(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                LogService.LogWarning("Tentativa de carregar pasta/vídeo com caminho vazio ou nulo.");
                return;
            }
            LogService.Log($"Iniciando carregamento de pasta/vídeo: {path}");

            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(path))
                    {
                        var newItem = CarregarPastaRecursivaSeNaoExistir(path, TreeRoot);
                        if (newItem != null)
                        {
                            Application.Current.Dispatcher.Invoke(() => TreeRoot.Add(newItem));
                        }
                    }
                    else if (File.Exists(path) && EhVideo(path))
                    {
                        var newItem = AdicionarVideoRecursivoSeNaoExistir(path, TreeRoot);
                        if (newItem != null)
                        {
                            Application.Current.Dispatcher.Invoke(() => TreeRoot.Add(newItem));
                        }
                    }
                });
                
                SaveTreeViewEstado();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao adicionar pasta ou vídeo: {ex.Message}", ex);
                throw; // Relança a exceção para que o chamador possa tratá-la
            }
            finally
            {
                LogService.Log($"[DEBUG] AddFolderOrVideo concluído. TreeRoot.Count: {TreeRoot.Count}");
            }
        }

        public void RemoveFolder(FolderItem? selectedFolder)
        {
            if (selectedFolder == null)
            {
                LogService.LogWarning("Tentativa de remover pasta selecionada, mas nenhuma pasta foi selecionada.");
                return;
            }
            LogService.Log($"Comando 'Remover Pasta Selecionada' acionado para: {selectedFolder.FullPath}");

            RemoverDoHashSetRecursivo(selectedFolder);

            if (selectedFolder.ParentFolder == null)
            {
                TreeRoot.Remove(selectedFolder);
            }
            else
            {
                selectedFolder.ParentFolder.Children.Remove(selectedFolder);
                AtualizarPais(selectedFolder.ParentFolder);
            }

            SaveTreeViewEstado();
            // AtualizarProgresso(); // This will be called from MainWindowViewModel
        }

        public IEnumerable<VideoItem> GetAllVideosRecursive(object item)
        {
            if (item is VideoItem v) yield return v;
            else if (item is FolderItem f)
                foreach (var child in f.Children)
                    foreach (var vid in GetAllVideosRecursive(child))
                        yield return vid;
        }

        public IEnumerable<VideoItem> GetVideosRecursive(FolderItem folder)
        {
            foreach (var child in folder.Children)
            {
                if (child is VideoItem v)
                {
                    yield return v;
                }
                else if (child is FolderItem f)
                {
                    foreach (var vid in GetVideosRecursive(f))
                    {
                        yield return vid;
                    }
                }
            }
        }

        public VideoItem? GetNextUnwatchedVideo(FolderItem? startFolder = null)
        {
            IEnumerable<VideoItem> videos;
            if (startFolder != null)
            {
                videos = GetAllVideosRecursive(startFolder);
            }
            else
            {
                videos = TreeRoot.SelectMany(GetAllVideosRecursive);
            }
            return videos.FirstOrDefault(v => !v.IsChecked);
        }

        public void SaveTreeViewEstado()
        {
            if (_isInitializing) return;

            LogService.Log("Salvando estado da TreeView.");
            var estado = new TreeViewEstado();

            foreach (var item in TreeRoot)
                SalvarEstadoRecursivo(item, estado);

            _persistenceService.SaveTreeViewEstado(estado);
        }

        public async Task CarregarEstadoTreeView()
        {
            LogService.Log("Carregando estado da TreeView.");
            var estado = _persistenceService.LoadTreeViewEstado();
            if (estado == null)
            {
                LogService.Log("Nenhum estado da TreeView encontrado para carregar.");
                return;
            }

            LogService.Log($"[DEBUG] Estado da TreeView carregado. Pastas: {estado.Pastas.Count}");
            foreach (var folderPath in estado.Pastas)
            {
                LogService.Log($"[DEBUG] Pasta no estado: {folderPath}");
            }

            _isInitializing = true;
            try
            {
                TreeRoot.Clear(); // Adicionado para limpar a TreeRoot antes de carregar
                ItensCarregados.Clear(); // Limpar também o HashSet de itens carregados

                // Usar Task.Run para carregar as pastas em uma thread separada
                await Task.Run(() =>
                {
                    foreach (var folderPath in estado.Pastas)
                    {
                        if (Directory.Exists(folderPath) && !ItensCarregados.Contains(folderPath))
                        {
                            // Adicionar diretamente à TreeRoot, pois estamos em uma thread de background
                            // e a ObservableCollection será atualizada na UI thread após a conclusão do Task.Run
                            var folder = _CarregarPastaRecursivaInterno(folderPath, TreeRoot);
                            if (folder != null)
                            {
                                Application.Current.Dispatcher.Invoke(() => TreeRoot.Add(folder));
                            }
                        }
                    }
                });

                // Restaurar o estado dos itens após a TreeRoot ser populada
                foreach (var item in TreeRoot)
                    RestaurarEstadoRecursivo(item, estado);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public void SalvarEstadoVideosAssistidos()
        {
            LogService.Log("Salvando estado dos vídeos assistidos.");
            _persistenceService.SaveWatchedVideos(VideosAssistidos);
        }

        public void CarregarEstadoVideosAssistidos()
        {
            LogService.Log("Carregando estado dos vídeos assistidos.");
            VideosAssistidos = _persistenceService.LoadWatchedVideos();
        }

        public void AtualizarCheckboxFolder(FolderItem folder)
        {
            var (total, marcados) = ContarVideos(folder);
            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);
        }

        public void AtualizarPais(FolderItem? folder)
        {
            if (folder == null) return;

            var (total, marcados) = ContarVideos(folder);
            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);

            AtualizarNomeComProgresso(folder);
            AtualizarPais(folder.ParentFolder);
        }

        public void AtualizarNomeComProgresso(FolderItem folder)
        {
            var (total, marcados) = ContarVideos(folder);

            string baseName = folder.Name.Split('(')[0].Trim();
            folder.DisplayName = $"{baseName} ({marcados}/{total})";

            foreach (var item in folder.Children.OfType<FolderItem>())
                AtualizarNomeComProgresso(item);
        }

        private FolderItem? CarregarPastaRecursivaSeNaoExistir(string path, ObservableCollection<object> parent)
        {
            return _CarregarPastaRecursivaInterno(path, parent);
        }

        private void CarregarPasta(string path, ObservableCollection<object> parent, FolderItem? parentFolder)
        {
            if (!Directory.Exists(path)) return;

            foreach (var dir in OrdenarNumericamente(Directory.GetDirectories(path)))
            {
                try
                {
                    if (ItensCarregados.Contains(dir)) continue;

                    var folder = CriarFolderItem(dir, parentFolder);
                    CarregarPasta(dir, folder.Children, folder);

                    if (GetAllVideosRecursive(folder).Any() || folder.Children.OfType<FolderItem>().Any())
                    {
                        Application.Current.Dispatcher.Invoke(() => parent.Add(folder));
                        ItensCarregados.Add(dir);
                        AtualizarCheckboxFolder(folder);
                        AtualizarNomeComProgresso(folder);
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Erro ao carregar subpasta '{dir}' em '{path}': {ex.Message}", ex);
                }
            }

            foreach (var file in OrdenarNumericamente(Directory.GetFiles(path)).Where(EhVideo))
            {
                try
                {
                    if (!ItensCarregados.Contains(file))
                    {
                        Application.Current.Dispatcher.Invoke(() => parent.Add(CriarVideoItem(file, parentFolder)));
                        ItensCarregados.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Erro ao carregar arquivo de vídeo '{file}' em '{path}': {ex.Message}", ex);
                }
            }
        }

        private VideoItem? AdicionarVideoRecursivoSeNaoExistir(string path, ObservableCollection<object> parent)
        {
            if (ItensCarregados.Contains(path)) return null;

            var video = CriarVideoItem(path, null);
            ItensCarregados.Add(path);
            return video;
        }

        private FolderItem CriarFolderItem(string dir, FolderItem? parentFolder)
        {
            var folder = new FolderItem
            {
                Name = Path.GetFileName(dir) ?? "",
                FullPath = dir,
                Children = new ObservableCollection<object>(),
                ParentFolder = parentFolder,
                DisplayName = Path.GetFileName(dir) ?? "" // Inicializa DisplayName
            };

            folder.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FolderItem.IsChecked))
                {
                    if (folder.IsChecked.HasValue)
                        folder.MarcarFilhos(folder.IsChecked.Value);

                    AtualizarPais(folder.ParentFolder);
                    SaveTreeViewEstado();

                    // AtualizarProgresso(); // This will be called from MainWindowViewModel
                }
                else if (e.PropertyName == nameof(FolderItem.IsExpanded))
                {
                    SaveTreeViewEstado();
                }
            };
            return folder;
        }

        private VideoItem CriarVideoItem(string file, FolderItem? parentFolder)
        {
            var video = new VideoItem
            {
                Name = Path.GetFileName(file) ?? "",
                FullPath = file,
                ParentFolder = parentFolder,
                IsChecked = VideosAssistidos.Contains(file)
            };

            video.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(VideoItem.IsChecked))
                {
                    if (video.IsChecked) VideosAssistidos.Add(video.FullPath);
                    else VideosAssistidos.Remove(video.FullPath);

                    AtualizarPais(video.ParentFolder);
                    SalvarEstadoVideosAssistidos();
                    SaveTreeViewEstado();

                    // AtualizarProgresso(); // This will be called from MainWindowViewModel
                }
            };
            return video;
        }

        public (int total, int marked) ContarVideos(object item)
        {
            int total = 0, marcados = 0;

            if (item is VideoItem v)
            {
                total = 1;
                if (v.IsChecked) marcados = 1;
            }
            else if (item is FolderItem f)
            {
                foreach (var child in f.Children)
                {
                    var (childTotal, childMarked) = ContarVideos(child);
                    total += childTotal;
                    marcados += childMarked;
                }
            }
            return (total, marcados);
        }

        private void RemoverDoHashSetRecursivo(FolderItem folder)
        {
            ItensCarregados.Remove(folder.FullPath);

            foreach (var child in folder.Children)
            {
                if (child is FolderItem childFolder)
                {
                    RemoverDoHashSetRecursivo(childFolder);
                }
                else if (child is VideoItem childVideo)
                {
                    ItensCarregados.Remove(childVideo.FullPath);
                }
            }
        }

        private void SalvarEstadoRecursivo(object item, TreeViewEstado estado)
        {
            switch (item)
            {
                case FolderItem f:
                    if (f.ParentFolder == null) estado.Pastas.Add(f.FullPath);

                    if (f.IsExpanded) estado.PastasExpandidas.Add(f.FullPath);

                    foreach (var child in f.Children.OfType<FolderItem>())
                        SalvarEstadoRecursivo(child, estado);
                    break;
                case VideoItem v:
                    break;
            }
        }

        private void CarregarPastaRecursivaSincrona(string path, ObservableCollection<object> parent)
        {
            var folder = _CarregarPastaRecursivaInterno(path, parent);
            if (folder != null)
            {
                Application.Current.Dispatcher.Invoke(() => parent.Add(folder));
            }
        }

        private void RestaurarEstadoRecursivo(object item, TreeViewEstado estado)
        {
            switch (item)
            {
                case FolderItem f:
                    f.IsExpanded = estado.PastasExpandidas.Contains(f.FullPath);
                    AtualizarCheckboxFolder(f);
                    AtualizarNomeComProgresso(f);

                    foreach (var child in f.Children)
                        RestaurarEstadoRecursivo(child, estado);
                    break;
                case VideoItem v:
                    v.IsChecked = VideosAssistidos.Contains(v.FullPath);
                    break;
            }
        }

        private bool EhVideo(string path) => _configuracoesProvider().VideoExtensions.Contains((Path.GetExtension(path) ?? string.Empty).ToLowerInvariant());

        private IEnumerable<string> OrdenarNumericamente(IEnumerable<string> paths)
        {
            return paths.OrderBy(p =>
            {
                string name = Path.GetFileNameWithoutExtension(p) ?? "";
                return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
            }).ThenBy(Path.GetFileName);
        }

        private FolderItem? _CarregarPastaRecursivaInterno(string path, ObservableCollection<object> parent)
        {
            if (ItensCarregados.Contains(path)) return null;

            var folder = CriarFolderItem(path, null);
            CarregarPasta(path, folder.Children, folder);

            if (GetAllVideosRecursive(folder).Any() || folder.Children.OfType<FolderItem>().Any())
            {
                ItensCarregados.Add(path);
                AtualizarCheckboxFolder(folder);
                AtualizarNomeComProgresso(folder);
                return folder;
            }
            return null;
        }

        public IEnumerable<VideoItem> GetAllVideosFromPersistedState()
        {
            var allVideos = new List<VideoItem>();
            var estado = _persistenceService.LoadTreeViewEstado();
            var videosAssistidos = _persistenceService.LoadWatchedVideos(); // Carrega os vídeos assistidos

            if (estado == null)
            {
                LogService.LogWarning("GetAllVideosFromPersistedState: Nenhum estado da TreeView encontrado para carregar.");
                return allVideos;
            }

            // Para cada pasta raiz salva no estado
            foreach (var folderPath in estado.Pastas)
            {
                if (Directory.Exists(folderPath))
                {
                    // Recursivamente, adicione todos os vídeos desta pasta
                    AdicionarVideosDePastaRecursivamente(folderPath, allVideos, videosAssistidos);
                }
            }
            return allVideos;
        }

        private void AdicionarVideosDePastaRecursivamente(string currentPath, List<VideoItem> videoList, HashSet<string> videosAssistidos)
        {
            // Adicionar vídeos diretamente na pasta atual
            foreach (var file in Directory.GetFiles(currentPath).Where(EhVideo))
            {
                videoList.Add(new VideoItem
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    IsChecked = videosAssistidos.Contains(file)
                });
            }

            // Chamar recursivamente para subpastas
            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                AdicionarVideosDePastaRecursivamente(dir, videoList, videosAssistidos);
            }
        }

        public VideoItem? GetVideoItemByPath(string fullPath)
        {
            // Normalizar o caminho de entrada para garantir a comparação correta
            var normalizedFullPath = NormalizePath(fullPath);

            // Percorrer a TreeRoot para encontrar o VideoItem correspondente
            foreach (var item in TreeRoot)
            {
                var video = FindVideoItemRecursive(item, normalizedFullPath);
                if (video != null)
                {
                    return video;
                }
            }
            return null;
        }

        private VideoItem? FindVideoItemRecursive(object item, string normalizedFullPath)
        {
            if (item is VideoItem videoItem)
            {
                if (NormalizePath(videoItem.FullPath) == normalizedFullPath)
                {
                    return videoItem;
                }
            }
            else if (item is FolderItem folderItem)
            {
                foreach (var child in folderItem.Children)
                {
                    var foundVideo = FindVideoItemRecursive(child, normalizedFullPath);
                    if (foundVideo != null)
                    {
                        return foundVideo;
                    }
                }
            }
            return null;
        }

        // Método auxiliar para normalizar caminhos (pode ser movido para um utilitário se usado em mais lugares)
        private string NormalizePath(string path)
        {
            if (path.StartsWith("file:///"))
            {
                path = path.Substring("file:///".Length);
            }
            return Path.GetFullPath(Uri.UnescapeDataString(path));
        }
    }
}
