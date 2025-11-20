using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GerenciadorAulas.Converters
{
    public class VolumeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int volume)
            {
                if (volume == 0)
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/mute.png"));
                }
                else if (volume < 33)
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/low_volume.png"));
                }
                else if (volume < 66)
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/medium_volume.png"));
                }
                else
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/high_volume.png"));
                }
            }
            return null!; // Ou um ícone padrão para erro
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
