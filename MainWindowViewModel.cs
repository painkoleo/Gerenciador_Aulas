using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using GerenciadorAulas.Services; // Dependência!

namespace GerenciadorAulas
{

    // ----------------------------------------------------
    // VIEW MODEL PRINCIPAL
    // ----------------------------------------------------

    public class MainWindowViewModel : ViewModelBase
    {
        // ----------------------------------------------------
        // 🔹 MÉTODOS DE COMANDO (Implementações que estavam faltando)
        // ----------------------------------------------------

        private void BtnStop_Click()
        {
            IsManuallyStopped = true;

            // Finaliza a thread MPV se estiver rodando
            if (mpvProcess != null)
            {
                try
                {
                    mpvProcess.Kill();
                    mpvProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    LogService.Log($"Erro ao finalizar MPV: {ex.Message}");
                }
                finally
                {
                    mpvProcess?.Dispose();
                    mpvProcess = null;
                }
            }

            // Cancela qualquer tarefa de reprodução contínua
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }

            VideoAtual = "";
        }

        private async Task BtnNextVideo_Click()
        {
            // Obtém todos os vídeos que NÃO estão checados (não assistidos)
            var videosNaoAssistidos = TreeRoot.SelectMany(ObterVideosRecursivo)
                                             .Where(v => !v.IsChecked)
                                             .ToList();

            if (!videosNaoAssistidos.Any())
            {
                _windowManager?.ShowMessageBox("Todos os vídeos foram assistidos!");
                return;
            }

            // Pega o primeiro vídeo não assistido na ordem da TreeView
            var nextVideo = videosNaoAssistidos.FirstOrDefault();

            if (nextVideo != null)
            {
                // Garante que o estado seja false antes de iniciar a reprodução.
                IsManuallyStopped = false;
                await ReproduzirVideosAsync(new[] { nextVideo });
            }
        }

        private void BtnRefresh_Click()
        {
            // Limpa tudo
            TreeRoot.Clear();
            itensCarregados.Clear();
            VideosAssistidos.Clear();
            FolderProgressList.Clear();

            // Recarrega o estado salvo
            CarregarEstadoVideosAssistidos();
            CarregarEstadoTreeView();

            // Atualiza o progresso
            AtualizarProgresso();
        }

        private void RemoverPastaSelecionada(FolderItem? selectedFolder)
        {
            if (selectedFolder == null) return;

            // Remove do HashSet de itens carregados (recursivamente)
            RemoverDoHashSetRecursivo(selectedFolder);

            // Remove do TreeRoot (se for uma pasta raiz)
            if (selectedFolder.ParentFolder == null)
            {
                TreeRoot.Remove(selectedFolder);
            }
            else
            {
                // Se for subpasta, remove do pai
                selectedFolder.ParentFolder.Children.Remove(selectedFolder);
                AtualizarPais(selectedFolder.ParentFolder);
            }

            // Salva o novo estado
            SalvarEstadoTreeView();
            AtualizarProgresso();
            OnPropertyChanged(nameof(TotalFolders));
            OnPropertyChanged(nameof(TotalVideos));
        }

        private void RemoverDoHashSetRecursivo(FolderItem folder)
        {
            itensCarregados.Remove(folder.FullPath);

            foreach (var child in folder.Children)
            {
                if (child is FolderItem childFolder)
                {
                    RemoverDoHashSetRecursivo(childFolder);
                }
                else if (child is VideoItem childVideo)
                {
                    itensCarregados.Remove(childVideo.FullPath);
                }
            }
        }
        // ----------------------------------------------------
        // DEPENDÊNCIAS
        // ----------------------------------------------------
        private readonly IWindowManager _windowManager;
        private readonly IPersistenceService _persistenceService;


        // ----------------------------------------------------
        // CAMPOS PRIVADOS E PATHS
        // ----------------------------------------------------

        private CancellationTokenSource? cts;
        private Process? mpvProcess;

        private Configuracoes _configuracoes;
        private string _videoAtual = "";
        private double _progressoGeral = 0;
        private bool _isManuallyStopped = false;
        private bool _isLoading = false; // Novo: Para a barra de progresso (indeterminado)
        private bool _isDragging = false; // Novo: Para o feedback visual do Drag&Drop

        private readonly HashSet<string> itensCarregados = new HashSet<string>();
        private HashSet<string> VideosAssistidos { get; set; } = new HashSet<string>();

        // ----------------------------------------------------
        // COLEÇÕES E PROPRIEDADES OBSERVÁVEIS
        // ----------------------------------------------------
        public ObservableCollection<object> TreeRoot { get; } = new ObservableCollection<object>();
        public ObservableCollection<FolderProgressItem> FolderProgressList { get; } = new ObservableCollection<FolderProgressItem>();

        public Configuracoes Configuracoes
        {
            get => _configuracoes;
            set { _configuracoes = value; OnPropertyChanged(nameof(Configuracoes)); }
        }

        public string VideoAtual
        {
            get => _videoAtual;
            set { _videoAtual = value; OnPropertyChanged(nameof(VideoAtual)); }
        }

        public double ProgressoGeral
        {
            get => _progressoGeral;
            set { _progressoGeral = value; OnPropertyChanged(nameof(ProgressoGeral)); }
        }

        public bool IsManuallyStopped
        {
            get => _isManuallyStopped;
            set { _isManuallyStopped = value; OnPropertyChanged(nameof(IsManuallyStopped)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public bool IsDragging
        {
            get => _isDragging;
            set { _isDragging = value; OnPropertyChanged(nameof(IsDragging)); }
        }

        // Propriedades para o StackPanel do topo (stubs)
        public int TotalFolders => TreeRoot.OfType<FolderItem>().Count();
        public int TotalVideos => TreeRoot.SelectMany(ObterVideosRecursivo).Count();

        // ----------------------------------------------------
        // COMANDOS
        // ----------------------------------------------------
        public RelayCommand<VideoItem?> PlayVideoCommand { get; }
        public RelayCommand<object?> PlaySelectedItemCommand { get; } // NOVO COMANDO AQUI
        public RelayCommand<object?> NextVideoCommand { get; }
        public RelayCommand<object?> StopPlaybackCommand { get; }
        public RelayCommand<object?> RefreshListCommand { get; }
        public RelayCommand<string> AddFoldersCommand { get; }
        public RelayCommand<object?> ClearSelectedFolderCommand { get; }

        public RelayCommand<object?> OpenConfigCommand { get; }
        public RelayCommand<object?> BrowseFoldersCommand { get; }
        public RelayCommand<object?> ShowProgressCommand { get; }

        // ----------------------------------------------------
        // CONSTRUTORES
        // ----------------------------------------------------

        public MainWindowViewModel() : this(new StubWindowManager(), new StubPersistenceService())
        {
            // Construtor de Design-Time
        }

        public MainWindowViewModel(IWindowManager windowManager, IPersistenceService persistenceService)
        {
            _windowManager = windowManager;
            _persistenceService = persistenceService;

            // 1. Inicializa Paths (agora gerenciado pelo PersistenceService)
            // appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas");
            // if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            // estadoArquivo = Path.Combine(appDataDir, "videos_assistidos.json");
            // ultimoVideoArquivo = Path.Combine(appDataDir, "ultimo_video.json");
            // estadoTreeArquivo = Path.Combine(appDataDir, "estadoTreeView.json");

            // 2. Carrega Configurações/Estado
            _configuracoes = ConfigManager.Carregar();
            CarregarEstadoVideosAssistidos();
            CarregarEstadoTreeView();

            // 3. Inicializa Comandos

            PlayVideoCommand = new RelayCommand<VideoItem?>(async video =>
            {
                if (video != null)
                {
                    IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir
                    await ReproduzirVideosAsync(new[] { video });
                }
            });

            // NOVO COMANDO PARA TRATAR SELEÇÃO DE PASTA OU VÍDEO
            PlaySelectedItemCommand = new RelayCommand<object?>(async item =>
            {
                // CORREÇÃO: Reseta o estado de parada manual para permitir que o Play funcione
                // mesmo após um Stop/Pause.
                IsManuallyStopped = false;

                if (item is VideoItem video)
                {
                    // Caso 1: Item selecionado é um vídeo. Toca ele.
                    await ReproduzirVideosAsync(new[] { video });
                }
                else if (item is FolderItem folder)
                {
                    // Caso 2: Item selecionado é uma pasta. Tenta tocar o próximo vídeo DENTRO dela.
                    LogService.Log($"Play acionado em pasta: {folder.Name}. Buscando primeiro vídeo não assistido dentro.");

                    // Procura o primeiro vídeo não assistido dentro da pasta (recursivamente)
                    var nextVideoInFolder = ObterVideosRecursivo(folder)
                                                 .FirstOrDefault(v => !v.IsChecked);

                    if (nextVideoInFolder != null)
                    {
                        await ReproduzirVideosAsync(new[] { nextVideoInFolder });
                    }
                    else
                    {
                        _windowManager?.ShowMessageBox($"A pasta '{folder.Name}' já está completa ou não contém vídeos não assistidos.");
                    }
                }
                // Se for null ou outro tipo, ignora
            });
            // FIM NOVO COMANDO

            NextVideoCommand = new RelayCommand<object?>(async _ => await BtnNextVideo_Click());
            StopPlaybackCommand = new RelayCommand<object?>(_ => BtnStop_Click());
            RefreshListCommand = new RelayCommand<object?>(_ => BtnRefresh_Click());

            AddFoldersCommand = new RelayCommand<string>(path =>
            {
                if (path != null && Directory.Exists(path))
                    CarregarPastaDropOrAdd(path);
            });

            ClearSelectedFolderCommand = new RelayCommand<object?>(item => RemoverPastaSelecionada(item as FolderItem));

            OpenConfigCommand = new RelayCommand<object?>(_ => _windowManager.ShowConfigWindow(Configuracoes));

            BrowseFoldersCommand = new RelayCommand<object?>(_ =>
            {
                string? selectedPath = _windowManager.OpenFolderDialog();
                if (selectedPath != null)
                {
                    CarregarPastaDropOrAdd(selectedPath);
                }
            });

            ShowProgressCommand = new RelayCommand<object?>(_ => _windowManager.ShowFolderProgressWindow(this));

            AtualizarProgresso();
        }

        // ----------------------------------------------------
        // 🔹 Carregamento e Drag & Drop
        // ----------------------------------------------------

        public void CarregarPastaDropOrAdd(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            IsLoading = true; // Inicia o indicador de progresso

            Task.Run(() =>
            {
                if (Directory.Exists(path))
                    CarregarPastaRecursivaSeNaoExistir(path, TreeRoot);
                else if (File.Exists(path) && EhVideo(path))
                    AdicionarVideoRecursivoSeNaoExistir(path, TreeRoot);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SalvarEstadoTreeView();
                    AtualizarProgresso();
                    OnPropertyChanged(nameof(TotalFolders));
                    OnPropertyChanged(nameof(TotalVideos));
                    IsLoading = false; // Finaliza o indicador
                });
            });
        }

        private void CarregarPastaRecursivaSeNaoExistir(string path, ObservableCollection<object> parent)
        {
            if (itensCarregados.Contains(path)) return;

            var folder = CriarFolderItem(path, null);
            CarregarPasta(path, folder.Children, folder);

            if (ObterVideosRecursivo(folder).Any() || folder.Children.OfType<FolderItem>().Any())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    parent.Add(folder);
                    itensCarregados.Add(path);
                    AtualizarCheckboxFolder(folder);
                    AtualizarNomeComProgresso(folder);
                });
            }
        }

        // ... (outros métodos de carregamento de pastas e vídeos) ...

        private void CarregarPasta(string path, ObservableCollection<object> parent, FolderItem? parentFolder)
        {
            if (!Directory.Exists(path)) return;

            // Carregar subpastas
            foreach (var dir in OrdenarNumericamente(Directory.GetDirectories(path)))
            {
                try
                {
                    if (itensCarregados.Contains(dir)) continue;

                    var folder = CriarFolderItem(dir, parentFolder);
                    CarregarPasta(dir, folder.Children, folder);

                    if (ObterVideosRecursivo(folder).Any() || folder.Children.OfType<FolderItem>().Any())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            parent.Add(folder);
                            itensCarregados.Add(dir);
                            AtualizarCheckboxFolder(folder);
                            AtualizarNomeComProgresso(folder);
                        });
                    }
                }
                catch (Exception ex) { LogService.Log($"Erro ao carregar pasta {dir}: {ex.Message}"); }
            }

            // Carregar arquivos
            foreach (var file in OrdenarNumericamente(Directory.GetFiles(path)).Where(EhVideo))
            {
                try
                {
                    if (!itensCarregados.Contains(file))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            parent.Add(CriarVideoItem(file, parentFolder));
                            itensCarregados.Add(file);
                        });
                    }
                }
                catch (Exception ex) { LogService.Log($"Erro ao carregar arquivo {file}: {ex.Message}"); }
            }
        }

        private void AdicionarVideoRecursivoSeNaoExistir(string path, ObservableCollection<object> parent)
        {
            if (!itensCarregados.Contains(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    parent.Add(CriarVideoItem(path, null));
                    itensCarregados.Add(path);
                });
            }
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
                    SalvarEstadoTreeView();

                    Application.Current.Dispatcher.Invoke(AtualizarProgresso);
                }
                else if (e.PropertyName == nameof(FolderItem.IsExpanded))
                {
                    SalvarEstadoTreeView();
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
                    SalvarEstadoTreeView();

                    Application.Current.Dispatcher.Invoke(AtualizarProgresso);
                }
            };
            return video;
        }

        // ... (outros métodos auxiliares de progresso e TreeView) ...

        public void AtualizarProgresso()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int totalGeral = 0, marcadosGeral = 0;

                FolderProgressList.Clear();
                var rootFolders = TreeRoot.OfType<FolderItem>().ToList();

                foreach (var rootFolder in rootFolders)
                {
                    var (t, m) = ContarVideos(rootFolder);
                    totalGeral += t;
                    marcadosGeral += m;

                    var newItem = new FolderProgressItem
                    {
                        FullPath = rootFolder.FullPath,
                        Name = $"{rootFolder.Name} ({m}/{t})",
                        Progress = t == 0 ? 0 : (double)m / t * 100
                    };
                    FolderProgressList.Add(newItem);

                    AtualizarNomeComProgresso(rootFolder);
                }

                ProgressoGeral = totalGeral == 0 ? 0 : (double)marcadosGeral / totalGeral * 100;
                OnPropertyChanged(nameof(TotalVideos)); // Atualiza contagem no topo
            });
        }

        // ----------------------------------------------------
        // 🔹 Métodos de Reprodução MPV (com a correção do Stop)
        // ----------------------------------------------------
        public async Task ReproduzirVideosAsync(IEnumerable<VideoItem> videos)
        {
            // Validação do MPV antes de qualquer outra coisa
            if (!IsMpvPathValid())
            {
                _windowManager?.ShowMessageBox("O caminho para o executável do MPV não foi configurado ou é inválido. Por favor, configure-o agora.");
                _windowManager?.ShowConfigWindow(Configuracoes);

                // Re-valida após o usuário (potencialmente) corrigir o caminho.
                if (!IsMpvPathValid()) return;
            }

            // O comando Play (PlaySelectedItemCommand) já deve ter garantido que IsManuallyStopped = false.
            // REMOVIDA a checagem if (IsManuallyStopped), que estava bloqueando o Play após um Stop.

            // Mata qualquer processo MPV anterior (o que setará IsManuallyStopped = true)
            BtnStop_Click();

            // CORREÇÃO: Reseta o estado para false. Isso permite que:
            // 1. A reprodução comece.
            // 2. A lógica de reprodução contínua (Configuracoes.ReproducaoContinua) funcione.
            IsManuallyStopped = false;

            cts = new CancellationTokenSource();
            var videoList = videos.ToList();
            bool allVideosPlayed = true;

            try
            {
                await Task.Run(() => PlayVideosLista(videoList, cts.Token));
            }
            catch (OperationCanceledException)
            {
                allVideosPlayed = false;
            }
            catch (Exception ex)
            {
                // Verificação de nulidade para o WindowManager
                _windowManager?.ShowMessageBox($"Erro ao reproduzir vídeo: {ex.Message}");
                allVideosPlayed = false;
            }
            finally
            {
                VideoAtual = "";
                cts?.Dispose();
                cts = null;
            }

            if (Configuracoes.ReproducaoContinua && allVideosPlayed && !IsManuallyStopped)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await BtnNextVideo_Click());
            }
            else if (!Configuracoes.ReproducaoContinua && allVideosPlayed)
            {
                // Se a reprodução contínua não estiver ativa, define como parado ao final da lista.
                IsManuallyStopped = true;
            }
        }

        private void PlayVideosLista(List<VideoItem> videos, CancellationToken token)
        {
            foreach (var video in videos)
            {
                token.ThrowIfCancellationRequested();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    video.IsChecked = true;
                    SalvarUltimoVideo(video.FullPath);
                    VideoAtual = $"Reproduzindo: {video.Name}";
                });

                // A validação principal agora é feita em ReproduzirVideosAsync.
                // Esta é uma última verificação de segurança.
                if (!IsMpvPathValid())
                {
                    throw new InvalidOperationException("Caminho do MPV inválido.");
                }

                try
                {
                    string args = (Configuracoes.MPVFullscreen ? "--fullscreen " : "") + $"\"{video.FullPath}\"";

                    mpvProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = Configuracoes.MPVPath,
                        Arguments = args,
                        UseShellExecute = false
                    });

                    mpvProcess?.WaitForExit();

                    mpvProcess?.Dispose();
                    mpvProcess = null;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _windowManager?.ShowMessageBox($"Erro ao abrir o MPV: Verifique o caminho em Configurações. Erro: {ex.Message}");
                    });
                }
            }
        }

        private bool IsMpvPathValid()
        {
            return !string.IsNullOrEmpty(Configuracoes.MPVPath) && File.Exists(Configuracoes.MPVPath);
        }

        // Funções Auxiliares (mantidas para a funcionalidade da TreeView)
        private (int total, int marked) ContarVideos(object item)
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

        private void AtualizarCheckboxFolder(FolderItem folder)
        {
            var (total, marcados) = ContarVideos(folder);
            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);
        }

        private void AtualizarPais(FolderItem? folder)
        {
            if (folder == null) return;

            var (total, marcados) = ContarVideos(folder);
            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);

            AtualizarNomeComProgresso(folder);
            AtualizarPais(folder.ParentFolder);
        }

        private void AtualizarNomeComProgresso(FolderItem folder)
        {
            var (total, marcados) = ContarVideos(folder);

            string baseName = folder.Name.Split('(')[0].Trim();
            folder.DisplayName = $"{baseName} ({marcados}/{total})";

            foreach (var item in folder.Children.OfType<FolderItem>())
                AtualizarNomeComProgresso(item);
        }

        // ... (Implementações de persistência) ...

        private void SalvarEstadoVideosAssistidos()
        {
            _persistenceService.SaveWatchedVideos(VideosAssistidos);
        }

        private void CarregarEstadoVideosAssistidos()
        {
            VideosAssistidos = _persistenceService.LoadWatchedVideos();
        }

        private void SalvarEstadoTreeView()
        {
            var estado = new TreeViewEstado();

            foreach (var item in TreeRoot)
                SalvarEstadoRecursivo(item, estado);

            _persistenceService.SaveTreeViewEstado(estado);
        }

        private void SalvarEstadoRecursivo(object item, TreeViewEstado estado)
        {
            switch (item)
            {
                case FolderItem f:
                    if (f.ParentFolder == null) estado.Pastas.Add(f.FullPath);

                    if (f.IsExpanded) estado.PastasExpandidas.Add(f.FullPath);

                    foreach (var child in f.Children)
                        SalvarEstadoRecursivo(child, estado);
                    break;
                case VideoItem v:
                    break;
            }
        }

        private void CarregarEstadoTreeView()
        {
            var estado = _persistenceService.LoadTreeViewEstado();
            if (estado == null) return;

            // Carrega as pastas raiz
            foreach (var folderPath in estado.Pastas)
            {
                if (Directory.Exists(folderPath) && !itensCarregados.Contains(folderPath))
                {
                    // Precisa rodar em Task.Run, mas aqui chamamos a versão síncrona para inicialização
                    CarregarPastaRecursivaSincrona(folderPath, TreeRoot);
                }
            }

            // Restaura estados (expandido, checado)
            foreach (var item in TreeRoot)
                RestaurarEstadoRecursivo(item, estado);
        }

        // Versão Síncrona para inicialização (usada pelo CarregarEstadoTreeView)
        private void CarregarPastaRecursivaSincrona(string path, ObservableCollection<object> parent)
        {
            if (itensCarregados.Contains(path)) return;

            var folder = CriarFolderItem(path, null);
            CarregarPasta(path, folder.Children, folder);

            if (ObterVideosRecursivo(folder).Any() || folder.Children.OfType<FolderItem>().Any())
            {
                parent.Add(folder);
                itensCarregados.Add(path);
                // Não precisa atualizar o checkbox e nome aqui, pois RestaurarEstadoRecursivo fará isso
            }
        }

        private void RestaurarEstadoRecursivo(object item, TreeViewEstado estado)
        {
            switch (item)
            {
                case FolderItem f:
                    // Restaurar estado de expansão
                    f.IsExpanded = estado.PastasExpandidas.Contains(f.FullPath);

                    // Atualizar checkbox e nome (progresso)
                    AtualizarCheckboxFolder(f);
                    AtualizarNomeComProgresso(f);

                    foreach (var child in f.Children)
                        RestaurarEstadoRecursivo(child, estado);
                    break;
                case VideoItem v:
                    // Vídeos são restaurados implicitamente via VideosAssistidos, mas forçamos a atualização
                    v.IsChecked = VideosAssistidos.Contains(v.FullPath);
                    break;
            }
        }

        // ... (Outras Funções Auxiliares) ...

        private IEnumerable<VideoItem> ObterVideosRecursivo(object item)
        {
            if (item is VideoItem v) yield return v;
            else if (item is FolderItem f)
                foreach (var child in f.Children)
                    foreach (var vid in ObterVideosRecursivo(child))
                        yield return vid;
        }

        private bool EhVideo(string path) => new[] { ".mp4", ".mkv", ".avi", ".mov" }
                                                     .Contains(Path.GetExtension(path)?.ToLower());

        private IEnumerable<string> OrdenarNumericamente(IEnumerable<string> paths)
        {
            return paths.OrderBy(p =>
            {
                string name = Path.GetFileNameWithoutExtension(p) ?? "";
                return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
            }).ThenBy(Path.GetFileName);
        }

        private void SalvarUltimoVideo(string caminho)
        {
            _persistenceService.SaveLastPlayedVideo(caminho);
        }


    }
}
