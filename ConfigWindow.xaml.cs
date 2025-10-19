using System.Windows;
using Ookii.Dialogs.Wpf;

namespace GerenciadorAulas
{
    public partial class ConfigWindow : Window
    {
        private Configuracoes _config;

        public ConfigWindow(Configuracoes config)
        {
            InitializeComponent();
            _config = config;

            // Carregar valores atuais
            txtPastaPadrao.Text = _config.PastaPadrao;
            chkReproducaoContinua.IsChecked = _config.ReproducaoContinua;
            chkFullscreenMPV.IsChecked = _config.MPVFullscreen;
            txtMPVPath.Text = _config.MPVPath;
        }

        private void BtnSelecionarPasta_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecione a pasta padrão",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) == true)
                txtPastaPadrao.Text = dialog.SelectedPath;
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
            _config.PastaPadrao = txtPastaPadrao.Text;
            _config.ReproducaoContinua = chkReproducaoContinua.IsChecked ?? true;
            _config.MPVFullscreen = chkFullscreenMPV.IsChecked ?? true;
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
