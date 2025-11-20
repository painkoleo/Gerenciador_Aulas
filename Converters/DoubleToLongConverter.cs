using System;
using System.Globalization;
using System.Windows.Data;

namespace GerenciadorAulas.Converters
{
    public class DoubleToLongConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return (long)doubleValue;
            }
            return 0L;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long longValue)
            {
                return (double)longValue;
            }
            return 0.0;
        }
    }
}
