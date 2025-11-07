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

        public void ShowConfigWindow(Configuracoes config, MainWindowViewModel viewModel)
        {
            var configWindow = new ConfigWindow(config, viewModel)
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

        public bool ShowConfirmationDialog(string message)
        {
            var result = MessageBox.Show(Application.Current.MainWindow, message, "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public string? OpenFileDialog(string filter)
        {
            var dialog = new VistaOpenFileDialog
            {
                Filter = filter,
                Multiselect = false
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) == true)
            {
                return dialog.FileName;
            }
            return null;
        }

        public string? SaveFileDialog(string defaultFileName, string filter)
        {
            var dialog = new VistaSaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) == true)
            {
                return dialog.FileName;
            }
            return null;
        }
    }
}
