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
using GerenciadorAulas.Exceptions;
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

        // Propriedades e Comandos do Player
        public RelayCommand PlayPauseCommand { get; }
        public RelayCommand StopPlayerCommand { get; }
        public RelayCommand ToggleMuteCommand { get; }

        public int Volume
        {
            get => _mediaPlayerService.Volume;
            set
            {
                if (_mediaPlayerService.Volume != value)
                {
                    _mediaPlayerService.Volume = value;
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public bool IsPlaying => _mediaPlayerService.IsPlaying;

        public async Task InitializeMediaPlayerAsync()
        {
            await _mediaPlayerService.InitializeAsync();
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

            // Inicializa Comandos do Player
            PlayPauseCommand = new RelayCommand(async _ =>
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

            _mediaPlayerService.IsPlayingChanged += (sender, e) => OnPropertyChanged(nameof(IsPlaying)); // Assina o evento

            // 2. Carrega Configurações/Estado
            _configuracoes = ConfigManager.Carregar();
            _treeViewDataService.LoadInitialTree();
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

            AtualizarProgresso();
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
                        _treeViewDataService.LoadInitialTree();
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

        private void RestoreData()
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
                        _treeViewDataService.LoadInitialTree();
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
            LogService.Log($"[DEBUG] ReproduzirVideosAsync iniciado. IsManuallyStopped: {IsManuallyStopped}");
            var videoList = videos.ToList();
            if (!videoList.Any())
            {
                LogService.LogWarning("Tentativa de reproduzir vídeos, mas a lista de vídeos está vazia.");
                return;
            }
            LogService.Log($"Iniciando reprodução assíncrona de 1 vídeo(s). Primeiro vídeo: {videoList.FirstOrDefault()?.FullPath}");

            IsManuallyStopped = false; // Garante que a flag seja resetada antes de reproduzir
            _mediaPlayerService.Stop(); // Garante que a reprodução anterior seja parada e o _playbackCompletion seja completado

            foreach (var video in videoList)
            {
                LogService.Log($"[DEBUG] ReproduzirVideosAsync - Loop. IsManuallyStopped: {IsManuallyStopped}, Video: {video.Name}");
                if (IsManuallyStopped) break; // Interrompe se o usuário parou manualmente

                LogService.Log($"Reproduzindo vídeo: {video.FullPath}");
                VideoAtual = $"Reproduzindo: {video.Name}";

                // Marca como assistido e salva o último vídeo ANTES de reproduzir
                video.IsChecked = true;
                LogService.Log($"[DEBUG] Vídeo '{video.Name}' marcado como assistido. IsChecked: {video.IsChecked}");
                SalvarUltimoVideo(video.FullPath);
                _treeViewDataService.SalvarEstadoVideosAssistidos(); // Salva o estado dos vídeos assistidos
                _treeViewDataService.SaveTreeViewEstado(); // Salva o estado da TreeView (para atualizar checkboxes)

                try
                {
                    await _mediaPlayerService.PlayAsync(video);
                }
                catch (MpvPathNotConfiguredException ex)
                {
                    LogService.LogWarning(ex.Message);
                    _windowManager.ShowMessageBox(ex.Message + "\nPor favor, configure-o agora.");
                    _windowManager.ShowConfigWindow(Configuracoes, this);
                    IsManuallyStopped = true; // Stop further playback attempts
                    break; // Exit the loop
                }

                // Se a reprodução contínua estiver desativada, para após o primeiro vídeo
                if (!Configuracoes.ReproducaoContinua)
                {
                    // IsManuallyStopped = true; // Removido para permitir que o botão "Próximo Vídeo" funcione múltiplas vezes
                    break;
                }
            }

            await HandlePlaybackCompletion();
        }

        private Task HandlePlaybackCompletion()
        {
            LogService.Log($"[DEBUG] HandlePlaybackCompletion iniciado. Configuracoes.ReproducaoContinua: {Configuracoes.ReproducaoContinua}, IsManuallyStopped: {IsManuallyStopped}");
            VideoAtual = ""; // Limpa o vídeo atual após a reprodução

            if (Configuracoes.ReproducaoContinua && !IsManuallyStopped)
            {
                LogService.Log("[DEBUG] HandlePlaybackCompletion: Reprodução contínua ativada e não parada manualmente. Não há botão Próximo Vídeo.");
                // await Application.Current.Dispatcher.InvokeAsync(new Func<Task>(BtnNextVideo_Click)); // Removido
            }
            else if (IsManuallyStopped) // Se foi parado manualmente, apenas registra
            {
                LogService.Log("[DEBUG] HandlePlaybackCompletion: Reprodução parada manualmente.");
            }
            // Se ReproducaoContinua for false e não foi parado manualmente, não faz nada além de limpar VideoAtual.
            return Task.CompletedTask;
        }

        // Funções Auxiliares (mantidas para a funcionalidade da TreeView)
































        private void SalvarUltimoVideo(string caminho)
        {
            LogService.Log($"Salvando último vídeo reproduzido: {caminho}");
            _persistenceService.SaveLastPlayedVideo(caminho);
        }


    }
}
