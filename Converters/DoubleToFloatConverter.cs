using System;
using System.Globalization;
using System.Windows.Data;

namespace GerenciadorAulas.Converters
{
    public class DoubleToFloatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return (float)doubleValue;
            }
            return 0.0f; // Valor padrão ou tratamento de erro
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float floatValue)
            {
                return (double)floatValue;
            }
            return 0.0; // Valor padrão ou tratamento de erro
        }
    }
}
