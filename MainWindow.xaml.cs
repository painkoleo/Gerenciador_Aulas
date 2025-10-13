using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf; // diálogo de pasta moderno WPF

namespace GerenciadorAulas
{
    public class AulaItem
    {
        public string Nome { get; set; } = "";
        public string Caminho { get; set; } = "";
        public List<AulaItem> SubItens { get; set; } = new List<AulaItem>();
        public bool ÉModulo => SubItens.Count > 0;
        public bool Assistido { get; set; } = false;
    }

    public partial class MainWindow : Window
    {
        private List<AulaItem> rootData = new List<AulaItem>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // Diálogo moderno WPF
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecione a pasta principal",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != true) return;

            string path = dialog.SelectedPath;
            txtFolderPath.Text = path;

            try
            {
                rootData = CarregarPastas(path);
                treeModules.Items.Clear();

                foreach (var item in rootData)
                    treeModules.Items.Add(CriarTreeViewItem(item));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar pastas: {ex.Message}");
            }
        }

        private List<AulaItem> CarregarPastas(string path)
        {
            var lista = new List<AulaItem>();

            try
            {
                // Pastas
                var dirs = Directory.GetDirectories(path)
                    .OrderBy(d => int.TryParse(new string(Path.GetFileName(d).TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue)
                    .ThenBy(d => Path.GetFileName(d));

                foreach (var dir in dirs)
                {
                    var item = new AulaItem { Nome = Path.GetFileName(dir), Caminho = dir };
                    item.SubItens = CarregarPastas(dir);
                    lista.Add(item);
                }

                // Arquivos de vídeo
                var files = Directory.GetFiles(path)
                    .Where(f => new[] { ".mp4", ".mkv", ".avi", ".mov" }.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => int.TryParse(new string(Path.GetFileNameWithoutExtension(f).TakeWhile(char.IsDigit).ToArray()), out int n) ? n : int.MaxValue)
                    .ThenBy(f => Path.GetFileName(f));

                foreach (var file in files)
                    lista.Add(new AulaItem { Nome = Path.GetFileName(file), Caminho = file });
            }
            catch
            {
                // Ignora pastas sem permissão
            }

            return lista;
        }

        private TreeViewItem CriarTreeViewItem(AulaItem item)
        {
            var tvi = new TreeViewItem { Header = CriarHeader(item), Tag = item };

            if (item.ÉModulo)
            {
                tvi.Items.Add(null); // nó temporário para lazy loading
                tvi.Expanded += (s, e) =>
                {
                    if (tvi.Items.Count == 1 && tvi.Items[0] == null)
                    {
                        tvi.Items.Clear();
                        foreach (var sub in item.SubItens)
                            tvi.Items.Add(CriarTreeViewItem(sub));
                    }
                };
            }

            return tvi;
        }

        private StackPanel CriarHeader(AulaItem item)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            var cb = new CheckBox
            {
                IsChecked = item.Assistido,
                Margin = new Thickness(0, 0, 5, 0)
            };
            cb.Checked += (s, e) => item.Assistido = true;
            cb.Unchecked += (s, e) => item.Assistido = false;
            sp.Children.Add(cb);

            var tb = new TextBlock { Text = item.Nome };
            sp.Children.Add(tb);

            if (!item.ÉModulo)
            {
                var btn = new Button { Content = "▶", Margin = new Thickness(5, 0, 0, 0) };
                btn.Click += (s, e) =>
                {
                    AbrirVideoMPV(item.Caminho);
                    item.Assistido = true;
                    cb.IsChecked = true;
                };
                sp.Children.Add(btn);
            }

            return sp;
        }

        private void AbrirVideoMPV(string caminhoVideo)
        {
            string mpvPath = @"C:\Program Files (x86)\mpv\mpv.exe"; // ajuste conforme seu sistema
            if (!File.Exists(mpvPath))
            {
                MessageBox.Show("MPV não encontrado. Verifique o caminho.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = mpvPath,
                    Arguments = $"\"{caminhoVideo}\"",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o MPV: {ex.Message}");
            }
        }
    }
}
