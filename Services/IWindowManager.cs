using GerenciadorAulas.Models;
using GerenciadorAulas.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GerenciadorAulas.Services
{
    // Interface que abstrai as operações de UI/Janelas
    public interface IWindowManager
    {
        void ShowConfigWindow(Configuracoes config, MainWindowViewModel viewModel);
        string? OpenFolderDialog();
        void ShowMessageBox(string message);
        bool ShowConfirmationDialog(string message);
        string? OpenFileDialog(string filter);
        string? SaveFileDialog(string defaultFileName, string filter);
        CloudFile? ShowCloudBackupWindow();
    }

    // Implementação para o design-time
    public class StubWindowManager : IWindowManager
    {
        public void ShowConfigWindow(Configuracoes config, MainWindowViewModel viewModel) { }
        public string? OpenFolderDialog() => null;
        public void ShowMessageBox(string message) { }
        public bool ShowConfirmationDialog(string message) => false;
        public string? OpenFileDialog(string filter) => null;
        public string? SaveFileDialog(string defaultFileName, string filter) => null;
        public CloudFile? ShowCloudBackupWindow() => null;
    }
}
