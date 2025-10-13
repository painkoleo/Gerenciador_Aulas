using System.ComponentModel;

namespace GerenciadorAulas
{
    public class VideoItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";

        private bool _isChecked;
        public bool IsChecked
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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
