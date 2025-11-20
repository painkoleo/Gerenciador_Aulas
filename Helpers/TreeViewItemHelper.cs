using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace GerenciadorAulas.Helpers
{
    // Classe auxiliar para manipular propriedades internas do TreeViewItem
    public static class TreeViewItemHelper
    {
        // Propriedade anexa para forçar a cor do Expander (seta)
        public static readonly DependencyProperty ExpanderForegroundProperty =
            DependencyProperty.RegisterAttached("ExpanderForeground", typeof(Brush), typeof(TreeViewItemHelper),
                new PropertyMetadata(null, OnExpanderForegroundChanged));

        public static Brush GetExpanderForeground(DependencyObject obj)
        {
            return (Brush)obj.GetValue(ExpanderForegroundProperty);
        }

        public static void SetExpanderForeground(DependencyObject obj, Brush value)
        {
            obj.SetValue(ExpanderForegroundProperty, value);
        }

        private static void OnExpanderForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewItem item)
            {
                // Este é um hack. Procuramos o ToggleButton interno.
                item.Loaded += (s, ev) => FindAndSetExpanderColor(item, (Brush)e.NewValue);
            }
        }

        // Método recursivo para encontrar o ToggleButton dentro do Template
        private static void FindAndSetExpanderColor(DependencyObject parent, Brush brush)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ToggleButton toggleButton)
                {
                    // Altera a cor do ToggleButton para torná-lo visível
                    toggleButton.Foreground = brush;
                    return;
                }

                // Busca recursiva
                FindAndSetExpanderColor(child, brush);
            }
        }
    }
}
