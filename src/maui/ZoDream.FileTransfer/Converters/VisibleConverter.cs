using System.Globalization;
using ZoDream.Shared.Converters;

namespace ZoDream.FileTransfer.Converters
{
    internal class VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ToggleConverter.IsVisible(value, parameter);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
