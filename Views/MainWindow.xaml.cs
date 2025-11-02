using System.Windows;
using GerenciadorAulas.ViewModels;

namespace GerenciadorAulas.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Manipuladores de Drag & Drop
        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && DataContext is MainWindowViewModel viewModel)
                {
                    // Usa a função pública do ViewModel para lidar com o drop
                    await viewModel.CarregarPastaDropOrAdd(files[0]);
                    viewModel.IsDragging = false;
                }
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effects = isFile ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = isFile;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = false;
            }
        }
    }
}
