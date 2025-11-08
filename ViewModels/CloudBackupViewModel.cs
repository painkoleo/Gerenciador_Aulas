using GerenciadorAulas.Commands;
using GerenciadorAulas.Models;
using GerenciadorAulas.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GerenciadorAulas.ViewModels
{
    public class CloudBackupViewModel : ViewModelBase
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IWindowManager _windowManager;

        private ObservableCollection<CloudFile> _backups = new ObservableCollection<CloudFile>();
        public ObservableCollection<CloudFile> Backups
        {
            get => _backups;
            set { _backups = value; OnPropertyChanged(nameof(Backups)); }
        }

        private CloudFile? _selectedBackup;
        public CloudFile? SelectedBackup
        {
            get => _selectedBackup;
            set { _selectedBackup = value; OnPropertyChanged(nameof(SelectedBackup)); }
        }

        public AsyncRelayCommand LoadBackupsCommand { get; }
        public RelayCommand SelectBackupCommand { get; }

        public CloudBackupViewModel(ICloudStorageService cloudStorageService, IWindowManager windowManager)
        {
            _cloudStorageService = cloudStorageService;
            _windowManager = windowManager;

            LoadBackupsCommand = new AsyncRelayCommand(LoadBackupsAsync);
            SelectBackupCommand = new RelayCommand(SelectBackup, CanSelectBackup);
        }

        private bool CanSelectBackup(object? parameter)
        {
            return SelectedBackup != null;
        }

        private void SelectBackup(object? parameter)
        {
            // This will be handled by the window itself, setting DialogResult
        }

        private async Task LoadBackupsAsync()
        {
            try
            {
                Backups.Clear();
                var loadedBackups = await _cloudStorageService.ListBackupsAsync();
                foreach (var backup in loadedBackups)
                {
                    Backups.Add(backup);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Erro ao carregar backups da nuvem.", ex);
                _windowManager.ShowMessageBox($"Erro ao carregar backups: {ex.Message}");
            }
        }
    }
}
