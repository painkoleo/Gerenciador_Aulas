using System.ComponentModel;
using System.Linq;
using System.Windows;
using GerenciadorAulas.Models;

namespace GerenciadorAulas.Views
{
    public partial class ProgressWindow : Window, INotifyPropertyChanged
    {
        private FolderItem folder;
        private double progressPercent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public double ProgressPercent
        {
            get => progressPercent;
            set
            {
                if (progressPercent != value)
                {
                    progressPercent = value;
                    OnPropertyChanged(nameof(ProgressPercent));
                }
            }
        }

        public ProgressWindow(FolderItem folder)
        {
            InitializeComponent();
            DataContext = this;
            this.folder = folder;

            folder.PropertyChanged += Folder_PropertyChanged;
            AtualizarProgresso();
        }

        private void Folder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderItem.IsChecked) || e.PropertyName == nameof(FolderItem.DisplayName))
            {
                Dispatcher.Invoke(AtualizarProgresso);
            }
        }

        private void AtualizarProgresso()
        {
            int total = 0, marcados = 0;
            ContarVideos(folder, ref total, ref marcados);
            ProgressPercent = total == 0 ? 0 : (double)marcados / total * 100;
            OnPropertyChanged(nameof(ProgressPercent));
        }

        private void ContarVideos(FolderItem folder, ref int total, ref int marcados)
        {
            foreach (var item in folder.Children)
            {
                if (item is VideoItem v)
                {
                    total++;
                    if (v.IsChecked) marcados++;
                }
                else if (item is FolderItem f)
                    ContarVideos(f, ref total, ref marcados);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
