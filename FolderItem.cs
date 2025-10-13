using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GerenciadorAulas
{
    public class FolderItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";

        private bool? _isChecked;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }

        public ObservableCollection<object> Children { get; } = new ObservableCollection<object>();

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
