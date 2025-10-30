using System.ComponentModel;
using System.Windows;

namespace GerenciadorAulas
{
    public class VideoItem : INotifyPropertyChanged, IHaveFullPath
    {
        private bool _isChecked = false;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public FolderItem? ParentFolder { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            // Garante que a notificação aconteça no thread da UI
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
