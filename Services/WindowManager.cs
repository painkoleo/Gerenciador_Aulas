using System.Windows;
using GerenciadorAulas;
using GerenciadorAulas.ViewModels;
using GerenciadorAulas.Views;
using Ookii.Dialogs.Wpf;

namespace GerenciadorAulas.Services
{
    // A implementação concreta e unificada que lida com a UI de janelas/diálogos.
    public class WindowManager : IWindowManager
    {
        // Construtor vazio, não depende mais de uma janela específica.
        public WindowManager()
        {
        }

        public string? OpenFolderDialog()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecione a pasta de aulas",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            // Usa a janela principal da aplicação como "owner"
            if (dialog.ShowDialog(Application.Current.MainWindow) == true)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        public void ShowConfigWindow(Configuracoes config)
        {
            var configWindow = new ConfigWindow(config)
            {
                Owner = Application.Current.MainWindow
            };
            configWindow.ShowDialog();
        }

        public void ShowFolderProgressWindow(MainWindowViewModel viewModel)
        {
            var progressWindow = new FolderProgressWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };
            progressWindow.ShowDialog();
        }

        public void ShowMessageBox(string message)
        { 
            MessageBox.Show(Application.Current.MainWindow, message, "Gerenciador de Aulas", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
