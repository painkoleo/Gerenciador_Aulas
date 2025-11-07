using System.Windows;
using Ookii.Dialogs.Wpf;
using GerenciadorAulas;
using GerenciadorAulas.ViewModels;

namespace GerenciadorAulas.Views
{
    public partial class ConfigWindow : Window
    {
        private Configuracoes _config;

        public ConfigWindow(Configuracoes config, MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            _config = config;

            // Carregar valores atuais
            chkReproducaoContinua.IsChecked = _config.ReproducaoContinua;
            chkFullscreenMPV.IsChecked = _config.MPVFullscreen;
            chkMinimizeToTray.IsChecked = _config.MinimizeToTray;
            txtMPVPath.Text = _config.MPVPath;
        }


        private void BtnSelecionarMPV_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executável MPV|mpv.exe",
                Title = "Selecione o MPV.exe"
            };

            if (dialog.ShowDialog() == true)
                txtMPVPath.Text = dialog.FileName;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            _config.ReproducaoContinua = chkReproducaoContinua.IsChecked ?? true;
            _config.MPVFullscreen = chkFullscreenMPV.IsChecked ?? true;
            _config.MinimizeToTray = chkMinimizeToTray.IsChecked ?? false;
            _config.MPVPath = txtMPVPath.Text;

            ConfigManager.Salvar(_config);

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void chkReproducaoContinua_Checked(object sender, RoutedEventArgs e)
        {
            _config.ReproducaoContinua = true;
            ConfigManager.Salvar(_config);
        }

        private void chkReproducaoContinua_Unchecked(object sender, RoutedEventArgs e)
        {
            _config.ReproducaoContinua = false;
            ConfigManager.Salvar(_config);
        }
    }
}
