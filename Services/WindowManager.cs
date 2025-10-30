// Nome do arquivo: Services/WindowManager.cs

using System.Windows;
using GerenciadorAulas;
using Ookii.Dialogs.Wpf;

namespace GerenciadorAulas.Services
{
    // A implementação concreta que lida com a UI de janelas/diálogos.
    public class WindowManager : IWindowManager
    {
        private readonly Window _owner; // Referência à MainWindow para diálogos modais

        public WindowManager(Window owner)
        {
            _owner = owner;
        }

        public string? OpenFolderDialog()
        {
            // Lógica que estava em MainWindow.xaml.cs
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecione pastas para adicionar",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog(_owner) == true)
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        public void ShowConfigWindow(Configuracoes config)
        {
            var configWindow = new ConfigWindow(config);
            configWindow.Owner = _owner;
            configWindow.ShowDialog();
        }

        public void ShowFolderProgressWindow(MainWindowViewModel viewModel)
        {
            // Note que seu projeto tem FolderProgressWindow.xaml e ProgressWindow.xaml.
            // Vou usar FolderProgressWindow, baseado nos nomes no seu ViewModel.
            var progressWindow = new FolderProgressWindow(viewModel);
            progressWindow.Owner = _owner;
            progressWindow.ShowDialog();
        }

        public void ShowMessageBox(string message)
        {
            MessageBox.Show(_owner, message);
        }
    }
}
