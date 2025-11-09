using System;
using System.Windows;
using GerenciadorAulas;
using GerenciadorAulas.Models;
using GerenciadorAulas.ViewModels;
using GerenciadorAulas.Views;
using Ookii.Dialogs.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace GerenciadorAulas.Services
{
    // A implementação concreta e unificada que lida com a UI de janelas/diálogos.
    public class WindowManager : IWindowManager
    {
        private readonly IServiceProvider _serviceProvider;

        // Construtor vazio, não depende mais de uma janela específica.
        public WindowManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

        public CloudFile? ShowCloudBackupWindow()
        {
            var viewModel = _serviceProvider.GetRequiredService<CloudBackupViewModel>();
            var window = new CloudBackupWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            bool? result = window.ShowDialog();
            if (result == true && viewModel.SelectedBackup != null)
            {
                return viewModel.SelectedBackup;
            }
            return null;
        }
    }
}
