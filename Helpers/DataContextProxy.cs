using System.Windows;

namespace GerenciadorAulas.Helpers
{
    public class DataContextProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new DataContextProxy();
        }

        public object DataContext
        {
            get { return (object)GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        public static readonly DependencyProperty DataContextProperty =
            DependencyProperty.Register("DataContext", typeof(object), typeof(DataContextProxy), new UIPropertyMetadata(null));
    }
}