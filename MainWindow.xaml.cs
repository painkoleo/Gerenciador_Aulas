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

namespace GerenciadorAulas
{
    public partial class MainWindow : Window
    {
        private HashSet<string> videosAssistidos = new HashSet<string>();
        private string estadoArquivo = "videos_assistidos.json";

        public ObservableCollection<object> TreeRoot { get; set; } = new ObservableCollection<object>();
        public RelayCommand<string?> PlayCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PlayCommand = new RelayCommand<string?>(AbrirVideoMPV);

            CarregarEstado();
        }

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
                CarregarPasta(path, TreeRoot);
                AtualizarProgresso();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a pasta: {ex.Message}");
            }
        }

        private void CarregarPasta(string path, ObservableCollection<object> parent)
        {
            if (!Directory.Exists(path)) return;

            // Pastas
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
                    var folderName = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(folderName))
                        folderName = dir.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).Last();

                    var folder = new FolderItem
                    {
                        Name = folderName,
                        FullPath = dir
                    };

                    folder.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FolderItem.IsChecked))
                            MarcarTodosPorCaminho(folder.FullPath, folder.IsChecked == true);
                    };

                    CarregarPasta(dir, folder.Children);
                    parent.Add(folder);

                    AtualizarCheckboxFolder(folder);
                    AtualizarNomeComProgresso(folder);
                }
                catch { /* Ignorar pastas inacessíveis */ }
            }

            // Vídeos
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
                        IsChecked = videosAssistidos.Contains(file)
                    };

                    video.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(VideoItem.IsChecked))
                        {
                            if (video.IsChecked) videosAssistidos.Add(video.FullPath);
                            else videosAssistidos.Remove(video.FullPath);

                            SalvarEstado();
                            AtualizarProgresso();
                            AtualizarNomePastaDoVideo(video);
                        }
                    };

                    parent.Add(video);
                }
                catch { }
            }
        }

        private void AbrirVideoMPV(string? caminhoVideo)
        {
            if (string.IsNullOrEmpty(caminhoVideo)) return;

            // Marcar automaticamente
            var videoItem = EncontrarVideoItem(caminhoVideo, TreeRoot);
            if (videoItem != null && !videoItem.IsChecked)
            {
                videoItem.IsChecked = true;
                videosAssistidos.Add(videoItem.FullPath);
                SalvarEstado();
                AtualizarProgresso();
            }

            string mpvPath = @"C:\Program Files (x86)\mpv\mpv.exe";
            if (!File.Exists(mpvPath))
            {
                MessageBox.Show("MPV não encontrado. Verifique o caminho.");
                return;
            }

            try
            {
                var pasta = Path.GetDirectoryName(caminhoVideo);
                if (pasta == null) return;

                // Lista de vídeos ordenada
                var arquivos = Directory.GetFiles(pasta)
                    .Where(f => new[] { ".mp4", ".mkv", ".avi", ".mov" }
                    .Contains(Path.GetExtension(f)?.ToLower()))
                    .OrderBy(f =>
                    {
                        string name = Path.GetFileNameWithoutExtension(f) ?? "";
                        return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
                    })
                    .ThenBy(f => Path.GetFileName(f))
                    .ToList();

                int index = arquivos.IndexOf(caminhoVideo);

                // Passar apenas os arquivos como playlist, não o arquivo isolado
                string argumentos = string.Join(" ", arquivos.Select(a => $"\"{a}\""));

                Process.Start(new ProcessStartInfo
                {
                    FileName = mpvPath,
                    Arguments = argumentos,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o MPV: {ex.Message}");
            }
        }



        private VideoItem? EncontrarVideoItem(string caminho, ObservableCollection<object> items)
        {
            foreach (var item in items)
            {
                if (item is VideoItem v && v.FullPath == caminho) return v;
                if (item is FolderItem f)
                {
                    var res = EncontrarVideoItem(caminho, f.Children);
                    if (res != null) return res;
                }
            }
            return null;
        }

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

        private void MarcarTodosPorCaminho(string pasta, bool marcar)
        {
            void MarcarRecursivo(ObservableCollection<object> items)
            {
                foreach (var item in items)
                {
                    if (item is VideoItem v && v.FullPath.StartsWith(pasta))
                        v.IsChecked = marcar;
                    else if (item is FolderItem f)
                    {
                        if (f.FullPath.StartsWith(pasta))
                            f.IsChecked = marcar;

                        MarcarRecursivo(f.Children);
                    }
                }
            }

            MarcarRecursivo(TreeRoot);
            SalvarEstado();
            AtualizarProgresso();
        }

        private void AtualizarNomeComProgresso(FolderItem folder)
        {
            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);
            folder.Name = $"{folder.Name.Split('(')[0].Trim()} ({marcados}/{total})";
        }

        private void AtualizarNomePastaDoVideo(VideoItem video)
        {
            foreach (var item in TreeRoot)
            {
                if (item is FolderItem f && video.FullPath.StartsWith(f.FullPath))
                    AtualizarNomeComProgresso(f);
            }
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
    }
}
