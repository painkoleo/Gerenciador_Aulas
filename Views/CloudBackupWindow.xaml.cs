using System.Windows;
using GerenciadorAulas.ViewModels;

namespace GerenciadorAulas.Views
{
    public partial class CloudBackupWindow : Window
    {
        private CloudBackupViewModel _viewModel;

        public CloudBackupWindow(CloudBackupViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;

            // Carrega os backups quando a janela é aberta
            this.Loaded += async (sender, e) =>
            {
                _viewModel.LoadBackupsCommand.Execute(null);
            };
        }

        private void BtnRestaurar_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedBackup != null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
