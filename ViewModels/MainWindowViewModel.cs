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
using GerenciadorAulas.Commands;
using GerenciadorAulas.Models;
using GerenciadorAulas.Services; // Dependência!

namespace GerenciadorAulas.ViewModels
{

    // ----------------------------------------------------
    // VIEW MODEL PRINCIPAL
    // ----------------------------------------------------

    public class MainWindowViewModel : ViewModelBase
    {


        private void BtnStop_Click()
        {
            LogService.Log("Comando 'Parar Reprodução' acionado.");
            IsManuallyStopped = true;
            _mediaPlayerService.Stop();
            VideoAtual = "";
        }

        private async Task BtnNextVideo_Click()
        {
            LogService.Log("Comando 'Próximo Vídeo' acionado.");
            var nextVideo = _treeViewDataService.GetNextUnwatchedVideo();

            if (nextVideo != null)
            {
                IsManuallyStopped = false;
                await ReproduzirVideosAsync(new[] { nextVideo });
            }
            else
            {
                _windowManager?.ShowMessageBox("Todos os vídeos foram assistidos!");
            }
        }

        private void BtnRefresh_Click()
        {
            LogService.Log("Comando 'Atualizar Lista' acionado.");
            FolderProgressList.Clear(); // Clear only this, TreeRoot is managed by service

            _treeViewDataService.LoadInitialTree();

            AtualizarProgresso();
        }

        private void RemoverPastaSelecionada(FolderItem? selectedFolder)
        {
            if (selectedFolder == null)
            {
                LogService.LogWarning("Tentativa de remover pasta selecionada, mas nenhuma pasta foi selecionada.");
                return;
            }
            LogService.Log($"Comando 'Remover Pasta Selecionada' acionado para: {selectedFolder.FullPath}");

            _treeViewDataService.RemoveFolder(selectedFolder);

            AtualizarProgresso();
            OnPropertyChanged(nameof(TotalFolders));
            OnPropertyChanged(nameof(TotalVideos));
        }


        // ----------------------------------------------------
        // DEPENDÊNCIAS
        // ----------------------------------------------------
        private readonly IWindowManager _windowManager;
        private readonly IPersistenceService _persistenceService;
        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly ITreeViewDataService _treeViewDataService;


        // ----------------------------------------------------
        // CAMPOS PRIVADOS E PATHS
        // ----------------------------------------------------

        private Configuracoes _configuracoes;
        private string _videoAtual = "";
        private double _progressoGeral = 0;
        private bool _isManuallyStopped = false;
        private bool _isLoading = false; // Novo: Para a barra de progresso (indeterminado)
        private bool _isDragging = false; // Novo: Para o feedback visual do Drag&Drop

        // ----------------------------------------------------
        // COLEÇÕES E PROPRIEDADES OBSERVÁVEIS
        // ----------------------------------------------------
        public ObservableCollection<object> TreeRoot => _treeViewDataService.TreeRoot;
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
        public int TotalVideos => TreeRoot.SelectMany(_treeViewDataService.GetAllVideosRecursive).Count();

        // ----------------------------------------------------
        // COMANDOS
        // ----------------------------------------------------
        public RelayCommand<VideoItem?> PlayVideoCommand { get; }
        public AsyncRelayCommand<object?> PlaySelectedItemCommand { get; }
        public AsyncRelayCommand<object?> NextVideoCommand { get; }
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

        public MainWindowViewModel() : this(new StubWindowManager(), new StubPersistenceService(), new StubMediaPlayerService(), new StubTreeViewDataService())
        {
            // Construtor de Design-Time
        }

        public MainWindowViewModel(IWindowManager windowManager, IPersistenceService persistenceService, IMediaPlayerService mediaPlayerService, ITreeViewDataService treeViewDataService)
        {
            LogService.Log("MainWindowViewModel inicializado.");
            _windowManager = windowManager;
            _persistenceService = persistenceService;
            _mediaPlayerService = mediaPlayerService;
            _treeViewDataService = treeViewDataService;



            // 2. Carrega Configurações/Estado
            _configuracoes = ConfigManager.Carregar();
            _treeViewDataService.LoadInitialTree();

            // 3. Inicializa Comandos

            PlayVideoCommand = new RelayCommand<VideoItem?>(async video =>
            {
                if (video != null)
                {
                    LogService.Log($"Comando 'Reproduzir Vídeo' acionado para: {video.FullPath}");
                    IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir
                    await _mediaPlayerService.PlayAsync(video);
                    VideoAtual = $"Reproduzindo: {video.Name}";
                    video.IsChecked = true;
                    SalvarUltimoVideo(video.FullPath);
                }
                else
                {
                    LogService.LogWarning("Comando 'Reproduzir Vídeo' acionado, mas nenhum vídeo foi fornecido.");
                }
            });

            // NOVO COMANDO PARA TRATAR SELEÇÃO DE PASTA OU VÍDEO
            PlaySelectedItemCommand = new AsyncRelayCommand<object?>(async item =>
            {
                LogService.Log("Comando 'Reproduzir Item Selecionado' acionado.");

                IsManuallyStopped = false;

                if (item is VideoItem video)
                {
                    await ReproduzirVideosAsync(new[] { video });
                }
                else if (item is FolderItem folder)
                {
                    LogService.Log($"Play acionado em pasta: {folder.Name}. Buscando primeiro vídeo não assistido dentro.");

                    var nextVideoInFolder = _treeViewDataService.GetVideosRecursive(folder)
                                                 .FirstOrDefault(v => !v.IsChecked);

                    if (nextVideoInFolder != null)
                    {
                        LogService.Log($"Próximo vídeo não assistido encontrado na pasta '{folder.Name}': {nextVideoInFolder.FullPath}");
                        await ReproduzirVideosAsync(new[] { nextVideoInFolder });
                    }
                    else
                    {
                        LogService.LogWarning($"A pasta '{folder.Name}' já está completa ou não contém vídeos não assistidos.");
                        _windowManager?.ShowMessageBox($"A pasta '{folder.Name}' já está completa ou não contém vídeos não assistidos.");
                    }
                }
                else
                {
                    LogService.LogWarning("Comando 'Reproduzir Item Selecionado' acionado, mas o item é nulo ou de um tipo não suportado.");
                }
            });
            // FIM NOVO COMANDO

            NextVideoCommand = new AsyncRelayCommand<object?>(async _ => await BtnNextVideo_Click());
            StopPlaybackCommand = new RelayCommand<object?>(_ => BtnStop_Click());
            RefreshListCommand = new RelayCommand<object?>(_ => BtnRefresh_Click());

            AddFoldersCommand = new RelayCommand<string>(async path =>
            {
                if (path != null && Directory.Exists(path))
                {
                    LogService.Log($"Comando 'Adicionar Pastas' acionado com caminho: {path}");
                    await CarregarPastaDropOrAdd(path);
                }
                else
                {
                    LogService.LogWarning($"Comando 'Adicionar Pastas' acionado com caminho inválido ou nulo: {path ?? "null"}");
                }
            });

            ClearSelectedFolderCommand = new RelayCommand<object?>(item =>
            {
                LogService.Log("Comando 'Limpar Pasta Selecionada' acionado.");
                RemoverPastaSelecionada(item as FolderItem);
            });

            OpenConfigCommand = new RelayCommand<object?>(_ =>
            {
                LogService.Log("Comando 'Abrir Configurações' acionado.");
                _windowManager.ShowConfigWindow(Configuracoes);
            });

            BrowseFoldersCommand = new RelayCommand<object?>(async _ =>
            {
                LogService.Log("Comando 'Procurar Pastas' acionado.");
                string? selectedPath = _windowManager.OpenFolderDialog();
                if (selectedPath != null)
                {
                    LogService.Log($"Pasta selecionada via diálogo: {selectedPath}");
                    await CarregarPastaDropOrAdd(selectedPath);
                }
                else
                {
                    LogService.Log("Seleção de pasta cancelada pelo usuário.");
                }
            });

            ShowProgressCommand = new RelayCommand<object?>(_ =>
            {
                LogService.Log("Comando 'Mostrar Progresso' acionado.");
                _windowManager.ShowFolderProgressWindow(this);
            });

            AtualizarProgresso();
        }

        // ----------------------------------------------------
        // 🔹 Carregamento e Drag & Drop
        // ----------------------------------------------------

        public async Task CarregarPastaDropOrAdd(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                LogService.LogWarning("Tentativa de carregar pasta/vídeo com caminho vazio ou nulo.");
                return;
            }
            LogService.Log($"Iniciando carregamento de pasta/vídeo: {path}");
            IsLoading = true; // Inicia o indicador de progresso

            try
            {
                await _treeViewDataService.AddFolderOrVideo(path);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Erro ao carregar pasta ou vídeo via Drag&Drop/Adicionar: {ex.Message}", ex);
                _windowManager?.ShowMessageBox($"Erro ao carregar: {ex.Message}");
            }
            finally
            {
                AtualizarProgresso();
                OnPropertyChanged(nameof(TotalFolders));
                OnPropertyChanged(nameof(TotalVideos));
                IsLoading = false; // Finaliza o indicador
            }
        }















        public void AtualizarProgresso()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int totalGeral = 0, marcadosGeral = 0;

                FolderProgressList.Clear();
                var rootFolders = TreeRoot.OfType<FolderItem>().ToList();

                foreach (var rootFolder in rootFolders)
                {
                    var (t, m) = _treeViewDataService.ContarVideos(rootFolder);
                    totalGeral += t;
                    marcadosGeral += m;

                    var newItem = new FolderProgressItem
                    {
                        FullPath = rootFolder.FullPath,
                        Name = $"{rootFolder.Name} ({m}/{t})",
                        Progress = t == 0 ? 0 : (double)m / t * 100
                    };
                    FolderProgressList.Add(newItem);

                    _treeViewDataService.AtualizarNomeComProgresso(rootFolder);
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
            var videoList = videos.ToList();
            if (!videoList.Any())
            {
                LogService.LogWarning("Tentativa de reproduzir vídeos, mas a lista de vídeos está vazia.");
                return;
            }
            LogService.Log($"Iniciando reprodução assíncrona de {videoList.Count} vídeo(s). Primeiro vídeo: {videoList.FirstOrDefault()?.FullPath}");

            IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir

            foreach (var video in videoList)
            {
                if (IsManuallyStopped) break; // Interrompe se o usuário parou manualmente

                LogService.Log($"Reproduzindo vídeo: {video.FullPath}");
                VideoAtual = $"Reproduzindo: {video.Name}";

                await _mediaPlayerService.PlayAsync(video);

                // Marca como assistido e salva o último vídeo
                video.IsChecked = true;
                SalvarUltimoVideo(video.FullPath);

                // Se a reprodução contínua estiver desativada, para após o primeiro vídeo
                if (!Configuracoes.ReproducaoContinua)
                {
                    IsManuallyStopped = true;
                    break;
                }
            }

            await HandlePlaybackCompletion();
        }

        private async Task HandlePlaybackCompletion()
        {
            VideoAtual = ""; // Limpa o vídeo atual após a reprodução
            LogService.Log($"[MainWindowViewModel] HandlePlaybackCompletion: Configuracoes.ReproducaoContinua = {Configuracoes.ReproducaoContinua}, IsManuallyStopped = {IsManuallyStopped}");

            if (Configuracoes.ReproducaoContinua && !IsManuallyStopped)
            {
                LogService.Log("[MainWindowViewModel] HandlePlaybackCompletion: Chamando BtnNextVideo_Click para reprodução contínua.");
                await Application.Current.Dispatcher.InvokeAsync(new Func<Task>(BtnNextVideo_Click));
            }
            else if (!Configuracoes.ReproducaoContinua && !IsManuallyStopped)
            {
                IsManuallyStopped = true;
                LogService.Log("[MainWindowViewModel] HandlePlaybackCompletion: Reprodução contínua desativada, definindo IsManuallyStopped como true.");
            }
            else if (IsManuallyStopped)
            {
                LogService.Log("[MainWindowViewModel] HandlePlaybackCompletion: Reprodução parada manualmente.");
            }
        }

        // Funções Auxiliares (mantidas para a funcionalidade da TreeView)
































        private void SalvarUltimoVideo(string caminho)
        {
            LogService.Log($"Salvando último vídeo reproduzido: {caminho}");
            _persistenceService.SaveLastPlayedVideo(caminho);
        }


    }
}
