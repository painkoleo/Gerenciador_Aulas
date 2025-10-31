using System.Windows;
using GerenciadorAulas.Services; // Para o IWindowManager

namespace GerenciadorAulas
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // CORREÇÃO: Inicializa o DataContext injetando a dependência REAL.
            this.DataContext = new MainWindowViewModel(new WindowManager());
        }

        // Manipuladores de Drag & Drop
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && this.DataContext is MainWindowViewModel viewModel)
                {
                    // Usa a função pública do ViewModel para lidar com o drop
                    viewModel.CarregarPastaDropOrAdd(files[0]);
                    viewModel.IsDragging = false;
                }
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effects = isFile ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = isFile;
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.IsDragging = false;
            }
        }
    }
}
