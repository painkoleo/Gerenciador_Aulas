using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GerenciadorAulas.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Garante que a notificação aconteça no thread da UI (Dispatcher)
            if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }
    }
}
