using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace GerenciadorAulas
{
    public class FolderItem : INotifyPropertyChanged
    {
        private string _name = "";
        private bool? _isChecked = false;

        public string FullPath { get; set; } = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
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

        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Atualiza o nome com o progresso dos vídeos
        public void AtualizarProgresso()
        {
            int total = 0, marcados = 0;
            ContarVideos(this, ref total, ref marcados);

            if (total > 0)
                Name = $"{Path.GetFileName(FullPath)} ({marcados}/{total})";
            else
                Name = Path.GetFileName(FullPath);

            // Atualiza recursivamente os filhos
            foreach (var child in Children)
            {
                if (child is FolderItem f)
                    f.AtualizarProgresso();
            }
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
    }
}
