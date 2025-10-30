using System.Windows;
using Microsoft.Win32;
using System.IO;
// Este using permite acessar as classes Window/ViewModel do namespace raiz
using GerenciadorAulas;

namespace GerenciadorAulas.Services
{
    // Interface que abstrai as operações de UI/Janelas
    public interface IWindowManager
    {
        // Usa a classe Configuracoes que você já definiu
        void ShowConfigWindow(Configuracoes config);
        string? OpenFolderDialog();
        void ShowMessageBox(string message);
        void ShowFolderProgressWindow(MainWindowViewModel viewModel);
    }

    // Implementação para o design-time
    public class StubWindowManager : IWindowManager
    {
        public void ShowConfigWindow(Configuracoes config) { }
        public string? OpenFolderDialog() => null;
        public void ShowMessageBox(string message) { }
        public void ShowFolderProgressWindow(MainWindowViewModel viewModel) { }
    }

    // Implementação real (resolve o erro CS0103)
    public class RealWindowManager : IWindowManager
    {
        public void ShowConfigWindow(Configuracoes config)
        {
            // Usa a ConfigWindow que você já tem
            var configWindow = new ConfigWindow(config);
            // Define o Owner para manter as janelas agrupadas
            if (Application.Current.MainWindow != null)
            {
                configWindow.Owner = Application.Current.MainWindow;
            }
            configWindow.ShowDialog();
        }

        public string? OpenFolderDialog()
        {
            // Código para abrir seletor de pasta
            var dialog = new OpenFileDialog
            {
                Title = "Selecione a Pasta de Aulas",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Selecione a Pasta"
            };

            if (dialog.ShowDialog() == true)
            {
                return Path.GetDirectoryName(dialog.FileName) ?? null;
            }
            return null;
        }

        public void ShowMessageBox(string message)
        {
            MessageBox.Show(message, "Gerenciador de Aulas", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowFolderProgressWindow(MainWindowViewModel viewModel)
        {
            // Usa a FolderProgressWindow que você já tem
            var progressWindow = new FolderProgressWindow(viewModel);
            if (Application.Current.MainWindow != null)
            {
                 progressWindow.Owner = Application.Current.MainWindow;
            }
            progressWindow.ShowDialog();
        }
    }
}
