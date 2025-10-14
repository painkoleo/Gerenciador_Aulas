using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GerenciadorAulas
{
    public class FolderItem : INotifyPropertyChanged
    {
        private string name = "";
        private bool? isChecked = false;
        private string displayName = "";
        
        public string Name
        {
            get => name;
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        // Nome exibido com progresso (Módulo 1 (3/5))
        public string DisplayName
        {
            get => displayName;
            set { displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public string FullPath { get; set; } = "";
        public FolderItem? ParentFolder { get; set; }

        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();

        public bool? IsChecked
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
