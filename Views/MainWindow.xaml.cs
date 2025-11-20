using System.Windows;
using GerenciadorAulas.ViewModels;
using System.Windows.Forms;
using System.Drawing;
using LibVLCSharp.Shared;
using GerenciadorAulas.Services; // Adicionado

namespace GerenciadorAulas.Views
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;
        private readonly IMediaPlayerService _mediaPlayerService;

        public MainWindow(MainWindowViewModel viewModel, IMediaPlayerService mediaPlayerService)
        {
            InitializeComponent();
            DataContext = viewModel;
            InitializeNotifyIcon();
            _mediaPlayerService = mediaPlayerService;
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.InitializeMediaPlayerAsync();
                VideoView.MediaPlayer = ((EmbeddedVlcPlayerUIService)_mediaPlayerService).MediaPlayer;
            }
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
                // Salvar as configurações da janela antes de fechar
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Configuracoes.WindowLeft = Left;
                    viewModel.Configuracoes.WindowTop = Top;
                    viewModel.Configuracoes.WindowWidth = Width;
                    viewModel.Configuracoes.WindowHeight = Height;
                    viewModel.Configuracoes.WindowState = WindowState;
                    ConfigManager.Salvar(viewModel.Configuracoes);
                }

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

        private void Slider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.StartSeekCommand.Execute(null);
            }
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                // O comando SeekCommand já é executado via Interaction.Triggers no XAML
                viewModel.EndSeekCommand.Execute(null);
            }
        }
    }
}
