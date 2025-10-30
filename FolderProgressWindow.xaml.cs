using System.Windows;

namespace GerenciadorAulas
{
    public partial class FolderProgressWindow : Window
    {
        public FolderProgressWindow(object viewModelOrData)
        {
            InitializeComponent();
            // Define o ViewModel (MainWindowViewModel) como DataContext.
            DataContext = viewModelOrData;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
