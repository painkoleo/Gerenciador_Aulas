using GerenciadorAulas;
using GerenciadorAulas.ViewModels;

namespace GerenciadorAulas.Services
{
    // Interface que abstrai as operações de UI/Janelas
    public interface IWindowManager
    {
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
}
