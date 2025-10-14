using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GerenciadorAulas
{
    public class FolderItem : INotifyPropertyChanged
    {
        private bool? _isChecked;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
