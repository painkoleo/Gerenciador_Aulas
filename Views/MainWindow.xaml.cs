using System.Windows;
using GerenciadorAulas.ViewModels;
using System.Windows.Forms;
using System.Drawing;

namespace GerenciadorAulas.Views
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon("Resources/icon.ico"),
                Visible = false,
                Text = "Gerenciador de Aulas"
            };

            _notifyIcon.DoubleClick += (s, args) => RestoreWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Restaurar", null, (s, args) => RestoreWindow());
            contextMenu.Items.Add("Fechar", null, (s, args) => CloseApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            if (_notifyIcon != null) _notifyIcon.Visible = false;
        }

        private void CloseApplication()
        {
            _isExplicitClose = true;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose && DataContext is MainWindowViewModel vm && vm.Configuracoes.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                if (_notifyIcon != null) _notifyIcon.Visible = true;
            }
            else
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            base.OnClosing(e);
        }

        // Manipuladores de Drag & Drop
        private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0 && DataContext is MainWindowViewModel viewModel)
                {
                    // Usa a função pública do ViewModel para lidar com o drop
                    await viewModel.CarregarPastaDropOrAdd(files[0]);
                    viewModel.IsDragging = false;
                }
            }
        }

        private void Window_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop);
            e.Effects = isFile ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = isFile;
            }
        }

        private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = false;
            }
        }
    }
}
