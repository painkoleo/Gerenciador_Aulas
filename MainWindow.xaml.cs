using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private string estadoArquivo = "videos_assistidos.json";

        public ObservableCollection<object> TreeRoot { get; set; } = new ObservableCollection<object>();

        public RelayCommand<string?> PlayCommand { get; }

        private CancellationTokenSource? cts; // Controle de pausa

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PlayCommand = new RelayCommand<string?>(AbrirVideoMPVAsync);

            CarregarEstado();
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
                catch { /* Ignorar pastas inacessíveis */ }
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
                catch { /* Ignorar arquivos inacessíveis */ }
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

        #region Salvar / carregar estado
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

        #region Reprodução de vídeos assíncrona
        private async void AbrirVideoMPVAsync(string? caminhoVideo)
        {
            if (string.IsNullOrEmpty(caminhoVideo)) return;

            cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => PlayVideosAsync(caminhoVideo, cts.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Reprodução pausada!");
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
        }

        private void PlayVideosAsync(string caminhoVideo, CancellationToken token)
        {
            string pasta = Path.GetDirectoryName(caminhoVideo) ?? "";

            var videosNaPasta = TreeRoot.SelectMany(x => ObterVideosRecursivo(x))
                                        .Where(v => Path.GetDirectoryName(v.FullPath) == pasta)
                                        .OrderBy(v =>
                                        {
                                            string name = Path.GetFileNameWithoutExtension(v.FullPath) ?? "";
                                            return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
                                        })
                                        .ThenBy(v => Path.GetFileName(v.FullPath))
                                        .ToList();

            int indiceAtual = videosNaPasta.FindIndex(v => v.FullPath == caminhoVideo);
            if (indiceAtual == -1) return;

            for (int i = indiceAtual; i < videosNaPasta.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var video = videosNaPasta[i];

                // Atualiza checkbox e progresso na UI
                Dispatcher.Invoke(() =>
                {
                    video.IsChecked = true;
                    if (!videosAssistidos.Contains(video.FullPath))
                        videosAssistidos.Add(video.FullPath);
                    SalvarEstado();
                    AtualizarProgresso();
                });

                // Executa MPV
                try
                {
                    using var mpv = Process.Start(new ProcessStartInfo
                    {
                        FileName = @"C:\Program Files (x86)\mpv\mpv.exe",
                        Arguments = $"\"{video.FullPath}\"",
                        UseShellExecute = false
                    });

                    if (mpv != null)
                        mpv.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erro ao abrir o MPV: {ex.Message}");
                    });
                }
            }
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
    }
}
