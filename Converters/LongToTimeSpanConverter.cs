using System;
using System.Globalization;
using System.Windows.Data;

namespace GerenciadorAulas.Converters
{
    public class LongToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long milliseconds)
            {
                TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            return "00:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return (long)timeSpan.TotalMilliseconds;
            }
            return 0L;
        }
    }
}
