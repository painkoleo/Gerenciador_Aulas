using System.ComponentModel;

namespace GerenciadorAulas
{
    public class VideoItem : INotifyPropertyChanged
    {
        private string name = "";
        private bool isChecked = false;

        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string FullPath { get; set; } = "";
        public FolderItem? ParentFolder { get; set; }

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
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
