using System;
using System.Globalization;
using System.Windows.Data;

namespace DrogueriaMickey.Desktop.Converters
{
    public class ActiveToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "Desactivar" : "Activar";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}