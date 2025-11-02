using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GerenciadorAulas.Models
{
    public class FolderProgressItem : INotifyPropertyChanged
    {
        private string name = "";
        private double progress = 0;

        // Propriedade para identificação da pasta (chave)
        public string FullPath { get; set; } = string.Empty;

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Progress
        {
            get => progress;
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
