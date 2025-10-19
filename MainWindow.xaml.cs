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
        // =====================================================
        // 🔹 Campos de estado
        // =====================================================
        private HashSet<string> videosAssistidos = new HashSet<string>();
        private string? estadoArquivo;
        private string? pastaArquivo;
        private string? ultimoVideoArquivo;
        private CancellationTokenSource? cts;

        public ObservableCollection<object> TreeRoot { get; set; } = new ObservableCollection<object>();
        public RelayCommand<VideoItem?> PlayCommand { get; }

        private Configuracoes configuracoes;
        private bool _reproducaoContinua = true;

        // =====================================================
        // 🔹 Construtor
        // =====================================================
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            PlayCommand = new RelayCommand<VideoItem?>(video =>
            {
                if (video != null)
                    _ = ReproduzirVideosAsync(new[] { video });
            });

            InicializarArquivosConfiguracao();
            CarregarEstado();
            CarregarUltimaPasta();

            // Carregar configurações
            configuracoes = ConfigManager.Carregar();

            // Aplicar pasta padrão, se existir
            if (Directory.Exists(configuracoes.PastaPadrao))
            {
                txtFolderPath.Text = configuracoes.PastaPadrao;
                CarregarPastaComProgresso(configuracoes.PastaPadrao);
            }

            // Aplicar configuração de reprodução contínua
            _reproducaoContinua = configuracoes.ReproducaoContinua;
        }

        // =====================================================
        // 🔹 Inicialização de arquivos de configuração
        // =====================================================
        private void InicializarArquivosConfiguracao()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GerenciadorAulas");
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);

            estadoArquivo = Path.Combine(appData, "videos_assistidos.json");
            pastaArquivo = Path.Combine(appData, "ultima_pasta.json");
            ultimoVideoArquivo = Path.Combine(appData, "ultimo_video.json");
        }

        // =====================================================
        // 🔹 Evento da janela carregada
        // =====================================================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => CarregarUltimaPasta(), System.Windows.Threading.DispatcherPriority.Background);
        }

        // =====================================================
        // 🔹 Seleção de pasta
        // =====================================================
        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            string? path = AbrirDialogPasta("Selecione a pasta principal");
            if (path == null) return;

            txtFolderPath.Text = path;
            SalvarUltimaPasta(path);
            CarregarPastaComProgresso(path);
        }

        private string? AbrirDialogPasta(string descricao)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = descricao,
                UseDescriptionForTitle = true
            };
            return dialog.ShowDialog(this) == true ? dialog.SelectedPath : null;
        }

        private void CarregarPastaComProgresso(string path)
        {
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
                    CarregarPastaComProgresso(path);
                }
            }
            catch { }
        }

        private void SalvarUltimaPasta(string path)
        {
            try { File.WriteAllText(pastaArquivo!, path); }
            catch { }
        }

        // =====================================================
        // 🔹 Drag & Drop
        // =====================================================
        private void Window_DragOver(object sender, DragEventArgs e) =>
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                    CarregarPastaSeNaoExistir(path);
                else if (File.Exists(path) && EhVideo(path))
                    AdicionarVideoSeNaoExistir(path);
            }

            AtualizarProgresso();
        }

        private void TreeModules_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void TreeModules_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                    CarregarPastaSeNaoExistir(path);
                else if (File.Exists(path) && EhVideo(path))
                    AdicionarVideoSeNaoExistir(path);
            }

            AtualizarProgresso();
        }

        private bool EhVideo(string path) => new[] { ".mp4", ".mkv", ".avi", ".mov" }
                                              .Contains(Path.GetExtension(path)?.ToLower());

        private void CarregarPastaSeNaoExistir(string path)
        {
            bool jaExiste = TreeRoot.OfType<FolderItem>().Any(f => f.Name == Path.GetFileName(path));
            if (!jaExiste)
            {
                CarregarPasta(path, TreeRoot, null);
                txtFolderPath.Text = path;
                SalvarUltimaPasta(path);
            }
        }

        private void AdicionarVideoSeNaoExistir(string path)
        {
            bool jaExiste = TreeRoot.OfType<VideoItem>().Any(v => v.FullPath == path);
            if (!jaExiste)
            {
                TreeRoot.Add(new VideoItem
                {
                    Name = Path.GetFileName(path),
                    FullPath = path,
                    IsChecked = videosAssistidos.Contains(path)
                });
            }
        }

        // =====================================================
        // 🔹 Carregamento de pastas e vídeos
        // =====================================================
        private void CarregarPasta(string path, ObservableCollection<object> parent, FolderItem? parentFolder)
        {
            if (!Directory.Exists(path)) return;

            foreach (var dir in OrdenarNumericamente(Directory.GetDirectories(path)))
            {
                try
                {
                    var folder = CriarFolderItem(dir, parentFolder);
                    CarregarPasta(dir, folder.Children, folder);
                    parent.Add(folder);
                    AtualizarCheckboxFolder(folder);
                    AtualizarNomeComProgresso(folder);
                }
                catch { }
            }

            foreach (var file in OrdenarNumericamente(Directory.GetFiles(path)).Where(EhVideo))
            {
                try
                {
                    var video = CriarVideoItem(file, parentFolder);
                    parent.Add(video);
                }
                catch { }
            }
        }

        private FolderItem CriarFolderItem(string dir, FolderItem? parentFolder)
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

            return folder;
        }

        private VideoItem CriarVideoItem(string file, FolderItem? parentFolder)
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

            return video;
        }

        private IEnumerable<string> OrdenarNumericamente(IEnumerable<string> paths)
        {
            return paths.OrderBy(p =>
            {
                string name = Path.GetFileNameWithoutExtension(p) ?? "";
                return int.TryParse(new string(name.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue;
            }).ThenBy(Path.GetFileName);
        }

        // =====================================================
        // 🔹 Checkboxes e progresso
        // =====================================================
        private void AtualizarCheckboxFolder(FolderItem folder)
        {
            if (folder == null) return;

            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);

            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);
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
            folder.IsChecked = (marcados == 0 ? false : marcados == total ? true : null);

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

        // =====================================================
        // 🔹 Salvar / carregar estado
        // =====================================================
        private void SalvarEstado()
        {
            try { File.WriteAllText(estadoArquivo!, JsonConvert.SerializeObject(videosAssistidos.ToList())); }
            catch { }
        }

        private void CarregarEstado()
        {
            if (!File.Exists(estadoArquivo!)) return;

            try
            {
                var lista = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(estadoArquivo!));
                if (lista != null) videosAssistidos = new HashSet<string>(lista);
            }
            catch { }
        }

        // =====================================================
        // 🔹 Último vídeo
        // =====================================================
        private void SalvarUltimoVideo(string caminho)
        {
            try { File.WriteAllText(ultimoVideoArquivo!, caminho); }
            catch { }
        }

        // =====================================================
        // 🔹 Reprodução de vídeos (MPV fullscreen configurável)
        // =====================================================
        private async Task ReproduzirVideosAsync(IEnumerable<VideoItem> videos)
        {
            cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => PlayVideosLista(videos.ToList(), cts.Token));
            }
            catch (OperationCanceledException)
            {
                lblVideoAtual.Content = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reproduzir vídeo: {ex.Message}");
            }
        }

        private void PlayVideosLista(List<VideoItem> videos, CancellationToken token)
        {
            foreach (var video in videos)
            {
                token.ThrowIfCancellationRequested();

                Dispatcher.Invoke(() =>
                {
                    video.IsChecked = true;
                    if (!videosAssistidos.Contains(video.FullPath)) videosAssistidos.Add(video.FullPath);
                    SalvarEstado();
                    AtualizarProgresso();
                    SalvarUltimoVideo(video.FullPath);
                    lblVideoAtual.Content = $"Reproduzindo: {video.Name}";
                });

                try
                {
                    string args = (configuracoes.MPVFullscreen ? "--fullscreen " : "") + $"\"{video.FullPath}\"";

                    using var mpv = Process.Start(new ProcessStartInfo
                    {
                        FileName = configuracoes.MPVPath,
                        Arguments = args,
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
            if (item is VideoItem v) yield return v;
            else if (item is FolderItem f)
                foreach (var child in f.Children)
                    foreach (var vid in ObterVideosRecursivo(child))
                        yield return vid;
        }

        // =====================================================
        // 🔹 Próxima aula
        // =====================================================
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

                string ultimo = File.Exists(ultimoVideoArquivo!) ? File.ReadAllText(ultimoVideoArquivo!) : "";
                int indexUltimo = todosVideos.FindIndex(v => v.FullPath == ultimo);

                List<VideoItem> proximosVideos;
                if (_reproducaoContinua)
                {
                    proximosVideos = indexUltimo >= 0
                        ? todosVideos.Skip(indexUltimo + 1).ToList()
                        : todosVideos;
                }
                else
                {
                    proximosVideos = indexUltimo >= 0
                        ? todosVideos.Skip(indexUltimo + 1).Where(v => !v.IsChecked).Take(1).ToList()
                        : todosVideos.Where(v => !v.IsChecked).Take(1).ToList();
                }

                if (proximosVideos.Count == 0)
                {
                    MessageBox.Show("Todos os vídeos já foram assistidos.");
                    return;
                }

                await ReproduzirVideosAsync(proximosVideos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reproduzir próxima aula: {ex.Message}");
            }
        }

        // =====================================================
        // 🔹 Parar reprodução
        // =====================================================
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();

            foreach (var proc in Process.GetProcessesByName("mpv"))
            {
                try { proc.Kill(); } catch { }
            }

            lblVideoAtual.Content = "";
        }

        // =====================================================
        // 🔹 Botão Atualizar lista
        // =====================================================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string path = txtFolderPath.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Selecione uma pasta válida antes de atualizar.");
                return;
            }

            CarregarPastaComProgresso(path);
            MessageBox.Show("Lista de vídeos atualizada!");
        }

        // =====================================================
        // 🔹 Botão Configurações
        // =====================================================
        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            var window = new ConfigWindow(configuracoes)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                // Reaplicar pasta padrão
                if (Directory.Exists(configuracoes.PastaPadrao))
                {
                    txtFolderPath.Text = configuracoes.PastaPadrao;
                    CarregarPastaComProgresso(configuracoes.PastaPadrao);
                }

                // Atualizar reprodução contínua
                _reproducaoContinua = configuracoes.ReproducaoContinua;
            }
        }
    }
}
