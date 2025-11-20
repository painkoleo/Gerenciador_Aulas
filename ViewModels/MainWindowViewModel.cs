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
using System.Windows.Threading; // Adicionado para DispatcherTimer
using GerenciadorAulas.Commands;
using GerenciadorAulas.Models;
using GerenciadorAulas.Services;
using LibVLCSharp.Shared;

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
            SelectedTabIndex = 0; // Voltar para a aba "Aulas"
        }



        private async void BtnRefresh_Click()
        {
            LogService.Log("Comando 'Atualizar Lista' acionado.");
            FolderProgressList.Clear(); // Clear only this, TreeRoot is managed by service

            await _treeViewDataService.LoadInitialTree();

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
        private readonly ICloudStorageService _cloudStorageService;

        // ----------------------------------------------------
        // CAMPOS PRIVADOS E PATHS
        // ----------------------------------------------------

        private Configuracoes _configuracoes;
        private string _videoAtual = "";
        private double _progressoGeral = 0;
        private bool _isManuallyStopped = false;
        private bool _isLoading = false; // Novo: Para a barra de progresso (indeterminado)
        private bool _isDragging = false; // Novo: Para o feedback visual do Drag&Drop
        private bool _isProgressTabSelected = false; // Otimização: para lazy loading da aba de progresso

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
            set
            {
                if (_isManuallyStopped != value)
                {
                    LogService.Log($"[DEBUG] IsManuallyStopped alterado de {_isManuallyStopped} para {value}. Chamador: {new StackTrace().GetFrame(1)?.GetMethod()?.Name}");
                    _isManuallyStopped = value;
                    OnPropertyChanged(nameof(IsManuallyStopped));
                }
            }
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

        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();

        // Propriedades para o StackPanel do topo (stubs)
        public int TotalFolders => TreeRoot.OfType<FolderItem>().Count();
        public int TotalVideos => TreeRoot.SelectMany(_treeViewDataService.GetAllVideosRecursive).Count();

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(nameof(SelectedTabIndex)); }
        }

        // Propriedades e Comandos do Player
        public AsyncRelayCommand PlayPauseCommand { get; }
        public RelayCommand StopPlayerCommand { get; }
        public RelayCommand ToggleMuteCommand { get; }

        private int _currentVolume = 50; // Valor padrão

        public int Volume
        {
            get => _currentVolume;
            set
            {
                if (_currentVolume != value)
                {
                    _currentVolume = value;
                    OnPropertyChanged(nameof(Volume));
                    if (_mediaPlayerService != null)
                    {
                        _mediaPlayerService.Volume = value;
                    }
                }
            }
        }

        public bool IsPlaying => _mediaPlayerService.IsPlaying;

        private long _currentTime;
        public long CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(nameof(CurrentTime)); }
        }

        private long _totalTime;
        public long TotalTime
        {
            get => _totalTime;
            set { _totalTime = value; OnPropertyChanged(nameof(TotalTime)); }
        }

        private float _playbackPosition;
        public float PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition != value)
                {
                    _playbackPosition = value;
                    OnPropertyChanged(nameof(PlaybackPosition));
                }
            }
        }

        private bool _isSeeking;
        public bool IsSeeking
        {
            get => _isSeeking;
            set { _isSeeking = value; OnPropertyChanged(nameof(IsSeeking)); }
        }

        private DispatcherTimer _timer;

        public async Task InitializeMediaPlayerAsync()
        {
            await _mediaPlayerService.InitializeAsync();
            await _treeViewDataService.LoadInitialTree(); // Carrega a TreeView de forma assíncrona e aguarda
        }
        // ----------------------------------------------------
        // COMANDOS
        // ----------------------------------------------------
        public RelayCommand<VideoItem?> PlayVideoCommand { get; }
        public AsyncRelayCommand<object?> PlaySelectedItemCommand { get; }
        public RelayCommand<object?> StopPlaybackCommand { get; }
        public RelayCommand<object?> RefreshListCommand { get; }
        public RelayCommand<string> AddFoldersCommand { get; }
        public RelayCommand<object?> ClearSelectedFolderCommand { get; }

        public RelayCommand<object?> OpenConfigCommand { get; }
        public RelayCommand<object?> BrowseFoldersCommand { get; }
        public RelayCommand<IEnumerable<object?>?> MarkSelectedCommand { get; }
        public RelayCommand<IEnumerable<object?>?> UnmarkSelectedCommand { get; }
        public RelayCommand<object?> BackupCommand { get; }
        public RelayCommand<object?> RestoreCommand { get; }
        public AsyncRelayCommand<object?> BackupToCloudCommand { get; }
        public AsyncRelayCommand RestoreFromCloudCommand { get; }

        public RelayCommand StartSeekCommand { get; }
        public RelayCommand EndSeekCommand { get; }
        public RelayCommand<float> SeekCommand { get; }

        public RelayCommand PlayNextCommand { get; }
        public RelayCommand PlayPreviousCommand { get; }
        public RelayCommand ProgressTabSelectedCommand { get; }

        // ----------------------------------------------------
        // CONSTRUTORES
        // ----------------------------------------------------

        public MainWindowViewModel() : this(new StubWindowManager(), new StubPersistenceService(), new StubMediaPlayerService(), new StubTreeViewDataService(), new StubCloudStorageService())
        {
            // Construtor de Design-Time
        }

        public MainWindowViewModel(IWindowManager windowManager, IPersistenceService persistenceService, IMediaPlayerService mediaPlayerService, ITreeViewDataService treeViewDataService, ICloudStorageService cloudStorageService)
        {
            LogService.Log("MainWindowViewModel inicializado.");
            _windowManager = windowManager;
            _persistenceService = persistenceService;
            _mediaPlayerService = mediaPlayerService;
            _treeViewDataService = treeViewDataService;
            _cloudStorageService = cloudStorageService;

            // Aplicar o volume inicial ao serviço de mídia
            _mediaPlayerService.Volume = _currentVolume;

            // Aplicar o volume inicial ao serviço de mídia
            _mediaPlayerService.Volume = _currentVolume;

            // Inicializa Comandos do Player
            PlayPauseCommand = new AsyncRelayCommand(async () =>
            {
                if (_mediaPlayerService.MediaPlayer.Media == null) // Nenhum vídeo carregado
                {
                    // Tenta reproduzir o item selecionado na TreeView
                    if (SelectedItems.FirstOrDefault() is VideoItem selectedVideo)
                    {
                        await ReproduzirVideosAsync(new[] { selectedVideo });
                    }
                    else if (SelectedItems.FirstOrDefault() is FolderItem selectedFolder)
                    {
                        var nextVideoInFolder = _treeViewDataService.GetVideosRecursive(selectedFolder)
                                                     .FirstOrDefault(v => !v.IsChecked);
                        if (nextVideoInFolder != null)
                        {
                            await ReproduzirVideosAsync(new[] { nextVideoInFolder });
                        }
                        else
                        {
                            _windowManager?.ShowMessageBox($"A pasta '{selectedFolder.Name}' já está completa ou não contém vídeos não assistidos.");
                        }
                    }
                    else
                    {
                        _windowManager?.ShowMessageBox("Nenhum vídeo ou pasta selecionada para reproduzir.");
                    }
                }
                else
                {
                    _mediaPlayerService.PlayPause(); // Pausa/retoma o vídeo atual
                }
            });
            StopPlayerCommand = new RelayCommand(_ => BtnStop_Click()); // Chama o método que define IsManuallyStopped
            ToggleMuteCommand = new RelayCommand(_ => _mediaPlayerService.ToggleMute());

            _mediaPlayerService.IsPlayingChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(IsPlaying));
                if (IsPlaying)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }; // Assina o evento

            // Inicializa o timer para a barra de progresso
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500); // Atualiza a cada 500ms
            _timer.Tick += (sender, e) =>
            {
                if (!IsSeeking) // Só atualiza se o usuário não estiver arrastando o slider
                {
                    CurrentTime = _mediaPlayerService?.Time ?? 0;
                    TotalTime = _mediaPlayerService?.Length ?? 0;
                    PlaybackPosition = _mediaPlayerService?.Position ?? 0.0f;
                }
            };

            // 2. Carrega Configurações/Estado
            _configuracoes = ConfigManager.Carregar();
            // _treeViewDataService.LoadInitialTree(); // Removido do construtor, será chamado em InitializeMediaPlayerAsync
            LogService.Log($"[DEBUG] VideosAssistidos carregados: {_treeViewDataService.VideosAssistidos.Count} vídeos.");
            foreach (var videoPath in _treeViewDataService.VideosAssistidos)
            {
                LogService.Log($"[DEBUG] Vídeo assistido: {videoPath}");
            }

            // 3. Inicializa Comandos

            PlayVideoCommand = new RelayCommand<VideoItem?>(async video =>
            {
                if (video != null)
                {
                    LogService.Log($"Comando 'Reproduzir Vídeo' acionado para: {video.FullPath}");
                    IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir
                    await ReproduzirVideosAsync(new[] { video });
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
                _windowManager.ShowConfigWindow(Configuracoes, this);
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

            MarkSelectedCommand = new RelayCommand<IEnumerable<object?>?>(selectedItems =>
            {
                LogService.Log("Comando 'Marcar Selecionados' acionado.");
                if (selectedItems == null) return;

                foreach (var item in selectedItems)
                {
                    if (item is VideoItem video)
                    {
                        video.IsChecked = true;
                    }
                    else if (item is FolderItem folder)
                    {
                        folder.IsChecked = true;
                    }
                }
                AtualizarProgresso();
            });

            UnmarkSelectedCommand = new RelayCommand<IEnumerable<object?>?>(selectedItems =>
            {
                LogService.Log("Comando 'Desmarcar Selecionados' acionado.");
                if (selectedItems == null) return;

                foreach (var item in selectedItems)
                {
                    if (item is VideoItem video)
                    {
                        video.IsChecked = false;
                    }
                    else if (item is FolderItem folder)
                    {
                        folder.IsChecked = false;
                    }
                }
                AtualizarProgresso();
            });

            BackupCommand = new RelayCommand<object?>(_ => BackupData());
            RestoreCommand = new RelayCommand<object?>(_ => RestoreData());

            BackupToCloudCommand = new AsyncRelayCommand<object?>(BackupToCloudAsync);
            RestoreFromCloudCommand = new AsyncRelayCommand(RestoreFromCloudAsync);

            StartSeekCommand = new RelayCommand(_ => IsSeeking = true);
            EndSeekCommand = new RelayCommand(_ => IsSeeking = false);
            SeekCommand = new RelayCommand<float>(position =>
            {
                _mediaPlayerService.Position = position;
                CurrentTime = _mediaPlayerService.Time; // Atualiza o tempo atual imediatamente
            });

            PlayNextCommand = new RelayCommand(_ => _mediaPlayerService.PlayNext());
            PlayPreviousCommand = new RelayCommand(_ => _mediaPlayerService.PlayPrevious());

            ProgressTabSelectedCommand = new RelayCommand(_ =>
            {
                if (!_isProgressTabSelected)
                {
                    _isProgressTabSelected = true;
                    AtualizarProgresso();
                }
            });

            _mediaPlayerService.VideoEnded += (sender, video) =>
            {
                LogService.Log($"[DEBUG] MainWindowViewModel: Evento VideoEnded disparado para: {video.FullPath}");
                // Marcar o vídeo como assistido e salvar o estado
                video.IsChecked = true;
                SalvarUltimoVideo(video.FullPath);
                _treeViewDataService.SalvarEstadoVideosAssistidos();
                _treeViewDataService.SaveTreeViewEstado();
                AtualizarProgresso(); // Atualiza a UI para refletir o vídeo marcado
            };

            SelectedTabIndex = 0; // Define a aba "Aulas" como selecionada por padrão
        }

        // ----------------------------------------------------
        // 🔹 BACKUP E RESTAURAÇÃO
        // ----------------------------------------------------

        private async Task RestoreFromCloudAsync()
        {
            IsLoading = true;
            string tempDownloadPath = Path.Combine(Path.GetTempPath(), $"restore-temp-{Guid.NewGuid()}.zip");
            try
            {
                if (_windowManager.ShowConfirmationDialog("Restaurar um backup da nuvem substituirá todos os dados atuais (listas de pastas e vídeos assistidos).\n\nDeseja continuar?"))
                {
                    CloudFile? selectedBackup = _windowManager.ShowCloudBackupWindow();

                    if (selectedBackup != null)
                    {
                        // 1. Baixar backup da nuvem
                        await _cloudStorageService.DownloadBackupAsync(selectedBackup.Id, tempDownloadPath);

                        // 2. Restaurar dados locais
                        _persistenceService.RestoreData(tempDownloadPath);

                        // 3. Recarregar dados da aplicação
                        await _treeViewDataService.LoadInitialTree();
                        AtualizarProgresso();

                        _windowManager.ShowMessageBox($"Backup '{selectedBackup.Name}' restaurado com sucesso!");
                    }
                    else
                    {
                        _windowManager.ShowMessageBox("Nenhum backup selecionado para restauração.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Falha ao restaurar backup da nuvem.", ex);
                _windowManager.ShowMessageBox($"Ocorreu um erro ao restaurar o backup da nuvem: {ex.Message}");
            }
            finally
            {
                // 4. Limpar arquivo temporário
                if (File.Exists(tempDownloadPath))
                {
                    File.Delete(tempDownloadPath);
                }
                IsLoading = false;
            }
        }

        private async Task BackupToCloudAsync(object? _)
        {
            IsLoading = true;
            string tempBackupPath = Path.Combine(Path.GetTempPath(), $"backup-temp-{Guid.NewGuid()}.zip");
            try
            {
                // 1. Criar backup local temporário
                _persistenceService.BackupData(tempBackupPath);

                // 2. Fazer upload para a nuvem
                string fileName = $"backup-aulas-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
                await _cloudStorageService.UploadBackupAsync(tempBackupPath, fileName);

                _windowManager.ShowMessageBox("Backup enviado para o Google Drive com sucesso!");
            }
            catch (Exception ex)
            {
                LogService.LogError("Falha ao fazer backup para a nuvem.", ex);
                _windowManager.ShowMessageBox($"Ocorreu um erro ao enviar o backup para a nuvem: {ex.Message}");
            }
            finally
            {
                // 3. Limpar arquivo temporário
                if (File.Exists(tempBackupPath))
                {
                    File.Delete(tempBackupPath);
                }
                IsLoading = false;
            }
        }

        private void BackupData()
        {
            try
            {
                string defaultFileName = $"backup-aulas-{DateTime.Now:yyyy-MM-dd}.zip";
                string? destinationPath = _windowManager.SaveFileDialog(defaultFileName, "Arquivo Zip (*.zip)|*.zip");

                if (!string.IsNullOrEmpty(destinationPath))
                {
                    _persistenceService.BackupData(destinationPath);
                    _windowManager.ShowMessageBox("Backup criado com sucesso!");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Falha ao criar backup.", ex);
                _windowManager.ShowMessageBox($"Ocorreu um erro ao criar o backup: {ex.Message}");
            }
        }

        private async Task RestoreData()
        {
            try
            {
                if (_windowManager.ShowConfirmationDialog("Restaurar um backup substituirá todos os dados atuais (listas de pastas e vídeos assistidos).\n\nDeseja continuar?"))
                {
                    string? sourcePath = _windowManager.OpenFileDialog("Arquivo Zip (*.zip)|*.zip");

                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        _persistenceService.RestoreData(sourcePath);

                        // Recarregar dados
                        await _treeViewDataService.LoadInitialTree();
                        AtualizarProgresso();

                        _windowManager.ShowMessageBox("Backup restaurado com sucesso!");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Falha ao restaurar backup.", ex);
                _windowManager.ShowMessageBox($"Ocorreu um erro ao restaurar o backup: {ex.Message}");
            }
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
            if (!_isProgressTabSelected) return; // Otimização: Só atualiza se a aba de progresso já foi aberta

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int totalGeral = 0, marcadosGeral = 0;

                FolderProgressList.Clear();
                var rootFolders = TreeRoot.OfType<FolderItem>().ToList();

                foreach (var rootFolder in rootFolders)
                {
                    _treeViewDataService.AtualizarNomeComProgresso(rootFolder); // Garante que DisplayName está atualizado

                    var (totalVideosInFolder, markedVideosInFolder) = _treeViewDataService.ContarVideos(rootFolder);
                    totalGeral += totalVideosInFolder;
                    marcadosGeral += markedVideosInFolder;

                    var folderProgress = CreateFolderProgressItemRecursive(rootFolder);
                    FolderProgressList.Add(folderProgress);
                }

                ProgressoGeral = totalGeral == 0 ? 0 : (double)marcadosGeral / totalGeral * 100;
                OnPropertyChanged(nameof(TotalVideos)); // Atualiza contagem no topo
            });
        }

        private FolderProgressItem CreateFolderProgressItemRecursive(FolderItem folderItem)
        {
            var (total, marked) = _treeViewDataService.ContarVideos(folderItem);
            var folderProgress = new FolderProgressItem
            {
                FullPath = folderItem.FullPath,
                Name = folderItem.DisplayName, // Usa o DisplayName já atualizado
                Progress = total == 0 ? 0 : (double)marked / total * 100
            };

            foreach (var child in folderItem.Children)
            {
                if (child is FolderItem childFolder)
                {
                    _treeViewDataService.AtualizarNomeComProgresso(childFolder); // Garante que DisplayName está atualizado
                    folderProgress.Children.Add(CreateFolderProgressItemRecursive(childFolder));
                }
            }
            return folderProgress;
        }

        // ----------------------------------------------------
        // 🔹 Métodos de Reprodução MPV (com a correção do Stop)
        // ----------------------------------------------------
        public async Task ReproduzirVideosAsync(IEnumerable<VideoItem> videos)
        {
            LogService.Log($"[DEBUG] ReproduzirVideosAsync iniciado. IsManuallyStopped: {IsManuallyStopped}");
            var videoList = videos.ToList();
            if (!videoList.Any())
            {
                LogService.LogWarning("Tentativa de reproduzir vídeos, mas a lista de vídeos está vazia.");
                return;
            }
            
            IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir

            // Construir a playlist completa de vídeos não assistidos a partir do primeiro vídeo da lista fornecida
            var allVideos = _treeViewDataService.GetAllVideosFromPersistedState().ToList();
            var firstVideoToPlay = videoList.First(); // O primeiro vídeo que o usuário clicou
            
            // Normalizar o caminho do vídeo selecionado para comparação
            var normalizedFirstVideoPath = NormalizePath(firstVideoToPlay.FullPath);

            var startIndex = allVideos.FindIndex(v => NormalizePath(v.FullPath) == normalizedFirstVideoPath);
            if (startIndex == -1)
            {
                LogService.LogError($"[DEBUG] O vídeo '{firstVideoToPlay.FullPath}' (normalizado: '{normalizedFirstVideoPath}') não foi encontrado na lista completa de vídeos persistida.");
                _windowManager?.ShowMessageBox("O vídeo selecionado não foi encontrado na lista de vídeos persistida.");
                return;
            }

            var playlistToService = new List<VideoItem>();
            // Adicionar o vídeo clicado, mesmo que já esteja assistido, para garantir que ele seja o primeiro a tocar
            playlistToService.Add(allVideos[startIndex]);

            for (int i = startIndex + 1; i < allVideos.Count; i++)
            {
                if (!allVideos[i].IsChecked) // Adiciona apenas vídeos não assistidos subsequentes
                {
                    playlistToService.Add(allVideos[i]);
                }
            }

            if (!playlistToService.Any())
            {
                LogService.LogWarning("Nenhum vídeo não assistido encontrado para reproduzir na sequência.");
                _windowManager?.ShowMessageBox("Nenhum vídeo não assistido encontrado na sequência.");
                return;
            }

            LogService.Log($"Iniciando reprodução assíncrona de {playlistToService.Count} vídeo(s). Primeiro vídeo na playlist: {playlistToService.FirstOrDefault()?.FullPath}");
            
            // Definir a playlist e iniciar a reprodução
            await _mediaPlayerService.SetPlaylistAndPlayAsync(playlistToService, true);

            SelectedTabIndex = 2; // Mudar para a aba "Player"

            // A lógica de marcação de vídeo assistido e salvamento será movida para um evento do MediaPlayerService
            // ou para o HandlePlaybackCompletion, que será simplificado.
            // Por enquanto, vamos marcar o primeiro vídeo da playlist como assistido aqui.
            var currentPlayingVideo = playlistToService.FirstOrDefault();
            if (currentPlayingVideo != null)
            {
                VideoAtual = $"Reproduzindo: {currentPlayingVideo.Name}";
                currentPlayingVideo.IsChecked = true;
                LogService.Log($"[DEBUG] Vídeo '{currentPlayingVideo.Name}' marcado como assistido. IsChecked: {currentPlayingVideo.IsChecked}");
                SalvarUltimoVideo(currentPlayingVideo.FullPath);
                _treeViewDataService.SalvarEstadoVideosAssistidos();
                _treeViewDataService.SaveTreeViewEstado();
            }
        }

        private void HandlePlaybackCompletion()
        {
            LogService.Log($"[DEBUG] HandlePlaybackCompletion iniciado. Configuracoes.ReproducaoContinua: {Configuracoes.ReproducaoContinua}, IsManuallyStopped: {IsManuallyStopped}");
            VideoAtual = ""; // Limpa o vídeo atual após a reprodução

            if (Configuracoes.ReproducaoContinua && !IsManuallyStopped)
            {
                LogService.Log("[DEBUG] HandlePlaybackCompletion: Reprodução contínua ativada e não parada manualmente. Verificando próximo vídeo na playlist do serviço de mídia.");
                // A lógica de avanço para o próximo vídeo agora é tratada pelo IMediaPlayerService
                // Este método pode ser usado para qualquer lógica pós-reprodução de um item da playlist,
                // como marcar o vídeo atual como assistido, etc.
                // No entanto, a chamada para PlayNext() já está no EndReached do EmbeddedVlcPlayerUIService.
                // Então, aqui podemos apenas garantir que o estado do ViewModel seja atualizado.
            }
            else if (IsManuallyStopped) // Se foi parado manualmente, apenas registra
            {
                LogService.Log("[DEBUG] HandlePlaybackCompletion: Reprodução parada manualmente.");
            }
            // Se ReproducaoContinua for false e não foi parado manualmente, não faz nada além de limpar VideoAtual.
        }

        // Funções Auxiliares (mantidas para a funcionalidade da TreeView)
































        private void SalvarUltimoVideo(string caminho)
        {
            LogService.Log($"Salvando último vídeo reproduzido: {caminho}");
            _persistenceService.SaveLastPlayedVideo(caminho);
        }

        private string NormalizePath(string path)
        {
            // Remove o prefixo "file:///" se presente
            if (path.StartsWith("file:///"))
            {
                path = path.Substring("file:///".Length);
            }
            // Decodifica caracteres especiais (ex: %20 para espaço)
            path = Uri.UnescapeDataString(path);
            // Garante o formato correto do sistema de arquivos (ex: barras invertidas no Windows)
            return Path.GetFullPath(path);
        }
    }
}
