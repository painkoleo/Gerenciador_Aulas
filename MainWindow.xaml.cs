using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorAulas
{
    public partial class MainWindow : Window
    {
        private HashSet<string> videosAssistidos = new HashSet<string>();
        private string estadoArquivo;
        private string pastaArquivo;
        private string ultimoVideoArquivo;

        public ObservableCollection<object> TreeRoot { get; set; } = new ObservableCollection<object>();

        private CancellationTokenSource? cts;

        public RelayCommand<VideoItem?> PlayCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PlayCommand = new RelayCommand<VideoItem?>(AbrirVideoMPVAsync);

            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas");
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);

            estadoArquivo = Path.Combine(appData, "videos_assistidos.json");
            pastaArquivo = Path.Combine(appData, "ultima_pasta.json");
            ultimoVideoArquivo = Path.Combine(appData, "ultimo_video.json");

            CarregarEstado();
            CarregarUltimaPasta();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => CarregarUltimaPasta(), System.Windows.Threading.DispatcherPriority.Background);
        }

        #region Seleção de pasta e carregamento
        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecione a pasta principal",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != true) return;

            string path = dialog.SelectedPath;
            txtFolderPath.Text = path;

            SalvarUltimaPasta(path);

            TreeRoot.Clear();
            AtualizarProgresso();

            try
            {
                CarregarPasta(path, TreeRoot, null);
                AtualizarProgresso();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a pasta: {ex.Message}");
            }
        }

        private void CarregarUltimaPasta()
        {
            if (!File.Exists(pastaArquivo)) return;

            try
            {
                string path = File.ReadAllText(pastaArquivo);
                if (Directory.Exists(path))
                {
                    txtFolderPath.Text = path;
                    TreeRoot.Clear();
                    CarregarPasta(path, TreeRoot, null);
                    AtualizarProgresso();
                }
            }
            catch { }
        }

private void TreeModules_DragOver(object sender, DragEventArgs e)
{
    // Apenas arquivos ou pastas
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        e.Effects = DragDropEffects.Copy;
    else
        e.Effects = DragDropEffects.None;

    e.Handled = true;
}

private void TreeModules_Drop(object sender, DragEventArgs e)
{
    if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

    var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

    foreach (var path in paths)
    {
        if (Directory.Exists(path))
        {
            // Adiciona a pasta e todo seu conteúdo recursivamente
            AdicionarPastaRecursiva(path, TreeRoot, null);
        }
        else if (File.Exists(path) && new[] { ".mp4", ".mkv", ".avi", ".mov" }.Contains(Path.GetExtension(path)?.ToLower()))
        {
            // Adiciona vídeo isolado na raiz
            var video = new VideoItem
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                IsChecked = videosAssistidos.Contains(path)
            };
            TreeRoot.Add(video);
        }
    }

    AtualizarProgresso();
}


private void AdicionarPastaRecursiva(string path, ObservableCollection<object> parent, FolderItem? parentFolder)
{
    var folderItem = new FolderItem
    {
        Name = Path.GetFileName(path),
        Children = new ObservableCollection<object>(),
        ParentFolder = parentFolder
    };

    folderItem.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(FolderItem.IsChecked))
        {
            if (folderItem.IsChecked.HasValue)
                folderItem.MarcarFilhos(folderItem.IsChecked.Value);

            AtualizarPais(folderItem.ParentFolder);
            SalvarEstado();
            AtualizarProgresso();
        }
    };

    // Pastas
    foreach (var dir in Directory.GetDirectories(path)
                                  .OrderBy(d => d))
    {
        AdicionarPastaRecursiva(dir, folderItem.Children, folderItem);
    }

    // Arquivos de vídeo
    foreach (var file in Directory.GetFiles(path)
                                  .Where(f => new[] { ".mp4", ".mkv", ".avi", ".mov" }.Contains(Path.GetExtension(f)?.ToLower()))
                                  .OrderBy(f => f))
    {
        var video = new VideoItem
        {
            Name = Path.GetFileName(file),
            FullPath = file,
            ParentFolder = folderItem,
            IsChecked = videosAssistidos.Contains(file)
        };

        video.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(VideoItem.IsChecked))
            {
                if (video.IsChecked) videosAssistidos.Add(video.FullPath);
                else videosAssistidos.Remove(video.FullPath);

                AtualizarPais(video.ParentFolder);
                SalvarEstado();
                AtualizarProgresso();
            }
        };

        folderItem.Children.Add(video);
    }

    parent.Add(folderItem);
    AtualizarCheckboxFolder(folderItem);
    AtualizarNomeComProgresso(folderItem);
}

private void Window_DragOver(object sender, DragEventArgs e)
{
    // Apenas arquivos ou pastas
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
        e.Effects = DragDropEffects.Copy;
    else
        e.Effects = DragDropEffects.None;

    e.Handled = true;
}

private void Window_Drop(object sender, DragEventArgs e)
{
    if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

    var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

    foreach (var path in paths)
    {
        if (Directory.Exists(path))
        {
            // Evita duplicação
            bool jaExiste = TreeRoot.OfType<FolderItem>().Any(f => f.Name == Path.GetFileName(path));
            if (!jaExiste)
            {
                CarregarPasta(path, TreeRoot, null); // mantém a ordenação numérica
                txtFolderPath.Text = path;            // atualiza o textbox
                SalvarUltimaPasta(path);             // salva a última pasta
            }
        }
        else if (File.Exists(path) && new[] { ".mp4", ".mkv", ".avi", ".mov" }.Contains(Path.GetExtension(path)?.ToLower()))
        {
            bool jaExiste = TreeRoot.OfType<VideoItem>().Any(v => v.FullPath == path);
            if (!jaExiste)
            {
                var video = new VideoItem
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    IsChecked = videosAssistidos.Contains(path)
                };
                TreeRoot.Add(video);
            }
        }
    }

    AtualizarProgresso();
}

        private void SalvarUltimaPasta(string path)
        {
            try
            {
                File.WriteAllText(pastaArquivo, path);
            }
            catch { }
        }

        private void CarregarPasta(string path, ObservableCollection<object> parent, FolderItem? parentFolder)
        {
            if (!Directory.Exists(path)) return;

           var pastas = Directory.GetDirectories(path)
    .OrderBy(d =>
    {
        string name = Path.GetFileName(d) ?? "";
        return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
    })
    .ThenBy(d => Path.GetFileName(d));

            foreach (var dir in pastas)
            {
                try
                {
                    var folder = new FolderItem
                    {
                        Name = Path.GetFileName(dir) ?? "",
                        Children = new ObservableCollection<object>(),
                        ParentFolder = parentFolder
                    };

                    folder.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FolderItem.IsChecked))
                        {
                            if (folder.IsChecked.HasValue)
                                folder.MarcarFilhos(folder.IsChecked.Value);

                            AtualizarPais(folder.ParentFolder);
                            SalvarEstado();
                            AtualizarProgresso();
                        }
                    };

                    CarregarPasta(dir, folder.Children, folder);

                    parent.Add(folder);

                    AtualizarCheckboxFolder(folder);
                    AtualizarNomeComProgresso(folder);
                }
                catch { }
            }

            var arquivos = Directory.GetFiles(path)
                .Where(f => new[] { ".mp4", ".mkv", ".avi", ".mov" }.Contains(Path.GetExtension(f)?.ToLower()))
                .OrderBy(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f) ?? "";
                    return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
                })
                .ThenBy(f => Path.GetFileName(f));

            foreach (var file in arquivos)
            {
                try
                {
                    var video = new VideoItem
                    {
                        Name = Path.GetFileName(file) ?? "",
                        FullPath = file,
                        ParentFolder = parentFolder,
                        IsChecked = videosAssistidos.Contains(file)
                    };

                    video.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(VideoItem.IsChecked))
                        {
                            if (video.IsChecked) videosAssistidos.Add(video.FullPath);
                            else videosAssistidos.Remove(video.FullPath);

                            AtualizarPais(video.ParentFolder);
                            SalvarEstado();
                            AtualizarProgresso();
                        }
                    };

                    parent.Add(video);
                }
                catch { }
            }
        }
        #endregion

        #region Atualização de checkboxes e progresso
        private void AtualizarCheckboxFolder(FolderItem folder)
        {
            if (folder == null) return;

            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);

            if (total == 0 || marcados == 0)
                folder.IsChecked = false;
            else if (marcados == total)
                folder.IsChecked = true;
            else
                folder.IsChecked = null;
        }

        private void ContarVideos(FolderItem folder, ref int total, ref int marcados)
        {
            foreach (var item in folder.Children)
            {
                if (item is VideoItem v)
                {
                    total++;
                    if (v.IsChecked) marcados++;
                }
                else if (item is FolderItem f)
                    ContarVideos(f, ref total, ref marcados);
            }
        }

        private void AtualizarPais(FolderItem? folder)
        {
            if (folder == null) return;

            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);

            if (total == 0 || marcados == 0)
                folder.IsChecked = false;
            else if (marcados == total)
                folder.IsChecked = true;
            else
                folder.IsChecked = null;

            AtualizarNomeComProgresso(folder);

            AtualizarPais(folder.ParentFolder);
        }

        private void AtualizarProgresso()
        {
            int total = 0, marcados = 0;

            foreach (var item in TreeRoot)
            {
                if (item is FolderItem f) ContarVideos(f, ref total, ref marcados);
                else if (item is VideoItem v)
                {
                    total++;
                    if (v.IsChecked) marcados++;
                }
            }

            progressBar.Value = total == 0 ? 0 : (double)marcados / total * 100;
            lblProgress.Content = $"{(int)progressBar.Value}%";
        }

        private void AtualizarNomeComProgresso(FolderItem folder)
        {
            if (folder == null) return;

            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);
            folder.DisplayName = $"{folder.Name.Split('(')[0].Trim()} ({marcados}/{total})";

            foreach (var item in folder.Children)
                if (item is FolderItem f)
                    AtualizarNomeComProgresso(f);
        }
        #endregion

        #region Salvar/Carregar estado
        private void SalvarEstado()
        {
            try
            {
                File.WriteAllText(estadoArquivo, JsonConvert.SerializeObject(videosAssistidos.ToList()));
            }
            catch { }
        }

        private void CarregarEstado()
        {
            if (!File.Exists(estadoArquivo)) return;

            try
            {
                var lista = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(estadoArquivo));
                if (lista != null)
                    videosAssistidos = new HashSet<string>(lista);
            }
            catch { }
        }
        #endregion

        #region Último vídeo
        private void SalvarUltimoVideo(string caminho)
        {
            try
            {
                File.WriteAllText(ultimoVideoArquivo, caminho);
            }
            catch { }
        }
        #endregion

        #region Reprodução de vídeos
        private async void AbrirVideoMPVAsync(VideoItem? videoItem)
        {
            if (videoItem == null) return;

            cts = new CancellationTokenSource();
            try
            {
                await Task.Run(() => PlayVideosListaAsync(new List<VideoItem> { videoItem }, cts.Token));
            }
            catch (OperationCanceledException)
            {
                // Cancelamento intencional — não exibe mensagem
                lblVideoAtual.Content = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reproduzir vídeo: {ex.Message}");
            }
        }

        private void PlayVideosListaAsync(List<VideoItem> videos, CancellationToken token)
        {
            foreach (var video in videos)
            {
                token.ThrowIfCancellationRequested();

                Dispatcher.Invoke(() =>
                {
                    video.IsChecked = true;
                    if (!videosAssistidos.Contains(video.FullPath))
                        videosAssistidos.Add(video.FullPath);
                    SalvarEstado();
                    AtualizarProgresso();
                    SalvarUltimoVideo(video.FullPath);
                    lblVideoAtual.Content = $"Reproduzindo: {video.Name}";
                });

                try
                {
                    using var mpv = Process.Start(new ProcessStartInfo
                    {
                        FileName = @"C:\Program Files (x86)\mpv\mpv.exe",
                        Arguments = $"\"{video.FullPath}\"",
                        UseShellExecute = false
                    });

                    mpv?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erro ao abrir o MPV: {ex.Message}");
                    });
                }
            }

            Dispatcher.Invoke(() => lblVideoAtual.Content = "");
        }

        private IEnumerable<VideoItem> ObterVideosRecursivo(object item)
        {
            if (item is VideoItem v)
                yield return v;
            else if (item is FolderItem f)
            {
                foreach (var child in f.Children)
                    foreach (var vid in ObterVideosRecursivo(child))
                        yield return vid;
            }
        }
        #endregion

        #region Botão Refresh
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string path = txtFolderPath.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Selecione uma pasta válida antes de atualizar.");
                return;
            }

            TreeRoot.Clear();
            try
            {
                CarregarPasta(path, TreeRoot, null);
                AtualizarProgresso();
                MessageBox.Show("Lista de vídeos atualizada!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar a lista: {ex.Message}");
            }
        }
        #endregion

        #region Botões Play/Pause/Stop
        private async void BtnPlayModule_Click(object sender, RoutedEventArgs e)
        {
            if (treeModules.SelectedItem == null)
            {
                MessageBox.Show("Selecione um módulo ou pasta na TreeView.");
                return;
            }

            List<VideoItem> videos = new List<VideoItem>();
            if (treeModules.SelectedItem is FolderItem folder)
                videos = ObterVideosRecursivo(folder).ToList();
            else if (treeModules.SelectedItem is VideoItem video)
                videos.Add(video);

            if (videos.Count == 0)
            {
                MessageBox.Show("Não há vídeos neste módulo.");
                return;
            }

            cts = new CancellationTokenSource();
            try
            {
                await Task.Run(() => PlayVideosListaAsync(videos, cts.Token));
            }
            catch (OperationCanceledException)
            {
                lblVideoAtual.Content = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reproduzir o módulo: {ex.Message}");
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();

            foreach (var proc in Process.GetProcessesByName("mpv"))
            {
                try { proc.Kill(); } catch { }
            }

            lblVideoAtual.Content = "";
        }
        #endregion

        #region Botão "Assistir próxima aula"
        private async void BtnNextVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var todosVideos = TreeRoot.SelectMany(v => ObterVideosRecursivo(v)).ToList();
                if (todosVideos.Count == 0)
                {
                    MessageBox.Show("Não há vídeos disponíveis.");
                    return;
                }

                string ultimo = File.Exists(ultimoVideoArquivo) ? File.ReadAllText(ultimoVideoArquivo) : "";
                int indexUltimo = todosVideos.FindIndex(v => v.FullPath == ultimo);

                List<VideoItem> proximosVideos;
                if (indexUltimo >= 0)
                    proximosVideos = todosVideos.Skip(indexUltimo + 1).Where(v => !v.IsChecked).ToList();
                else
                    proximosVideos = todosVideos.Where(v => !v.IsChecked).ToList();

                if (proximosVideos.Count == 0)
                {
                    MessageBox.Show("Todos os vídeos já foram assistidos.");
                    return;
                }

                cts = new CancellationTokenSource();
                await Task.Run(() => PlayVideosListaAsync(proximosVideos, cts.Token));
            }
            catch (OperationCanceledException)
            {
                // Cancelamento intencional — sem mensagem de erro
                lblVideoAtual.Content = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao tentar reproduzir próxima aula: {ex.Message}");
            }
        }
        #endregion
    }
}
