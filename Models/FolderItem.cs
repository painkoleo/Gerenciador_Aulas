using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace GerenciadorAulas.Models
{
    // Nota: Esta classe implementa INotifyPropertyChanged diretamente para permitir que objetos
    // File/Folder sejam marcados de forma segura, mesmo se a atualização vier de um thread em segundo plano.
    public class FolderItem : INotifyPropertyChanged, IHaveFullPath
    {
        private bool? _isChecked = false;
        private bool _isExpanded = false;
        private string _displayName = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();
        public FolderItem? ParentFolder { get; set; }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public bool? IsChecked
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

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public void MarcarFilhos(bool isChecked)
        {
            foreach (var child in Children.OfType<FolderItem>())
            {
                child.IsChecked = isChecked;
                child.MarcarFilhos(isChecked);
            }
            foreach (var child in Children.OfType<VideoItem>())
            {
                child.IsChecked = isChecked;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
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
