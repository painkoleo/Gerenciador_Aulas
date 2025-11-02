using System.Windows;
using System.Windows.Controls;

namespace GerenciadorAulas.Helpers
{
    // Esta classe estática adiciona a funcionalidade de TwoWay Binding ao SelectedItem do TreeView.
    public static class TreeViewExtensions
    {
        // 1. Cria a Attached Property que o ViewModel irá usar.
        public static readonly DependencyProperty BindableSelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItem",
                typeof(object),
                typeof(TreeViewExtensions),
                new FrameworkPropertyMetadata(null, OnBindableSelectedItemChanged));

        // Get e Set para a propriedade anexada.
        public static object GetBindableSelectedItem(DependencyObject obj)
        {
            return obj.GetValue(BindableSelectedItemProperty);
        }

        public static void SetBindableSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(BindableSelectedItemProperty, value);
        }

        // 2. Método de Callback para quando o ViewModel altera o SelectedItem.
        private static void OnBindableSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView && e.NewValue != null)
            {
                // Aqui você pode adicionar lógica para encontrar o item na TreeView
                // e expandir/scrollar até ele. Por enquanto, focamos na mudança.

                // NOTA: Para realmente *selecionar* programaticamente um item na TreeView,
                // você precisaria de um método de busca recursiva que encontre o
                // correspondente na UI (TreeViewItem) e defina IsSelected = true.
                // Como não temos a implementação dessa busca, vamos focar no caminho
                // inverso (UI -> ViewModel) primeiro, garantindo que o valor seja o mesmo.
            }
        }

        // 3. Cria a Attached Property para fazer a ligação inversa (UI -> ViewModel).
        public static readonly DependencyProperty IsMonitoringSelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "IsMonitoringSelectedItem",
                typeof(bool),
                typeof(TreeViewExtensions),
                new PropertyMetadata(false, OnIsMonitoringSelectedItemChanged));

        public static bool GetIsMonitoringSelectedItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMonitoringSelectedItemProperty);
        }

        public static void SetIsMonitoringSelectedItem(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMonitoringSelectedItemProperty, value);
        }

        // 4. Conecta o evento SelectedItemChanged da TreeView ao ViewModel.
        private static void OnIsMonitoringSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView && (bool)e.NewValue)
            {
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
            }
            else if (d is TreeView treeView2)
            {
                treeView2.SelectedItemChanged -= TreeView_SelectedItemChanged;
            }
        }

        // 5. Atualiza o ViewModel quando o usuário clica em um item.
        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView treeView)
            {
                // Seta o valor do BindableSelectedItem no TreeView para o novo item
                SetBindableSelectedItem(treeView, treeView.SelectedItem);
            }
        }
    }
}
