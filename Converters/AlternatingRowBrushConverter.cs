using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GerenciadorAulas.Converters
{
    public class AlternatingRowBrushConverter : IValueConverter
    {
        public Brush Brush1 { get; set; } = Brushes.Transparent;
        public Brush Brush2 { get; set; } = Brushes.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index % 2 == 0 ? Brush1 : Brush2;
            }
            return Brush1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
