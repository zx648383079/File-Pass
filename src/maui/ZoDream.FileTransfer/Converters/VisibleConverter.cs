using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Converters
{
    internal class VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }
            if (parameter is not null)
            {
                var pStr = parameter.ToString();
                var vStr = value.ToString();
                if (pStr == vStr)
                {
                    return true;
                }
                if (vStr is null || pStr is null || !pStr.Contains(','))
                {
                    return false;
                }
                return pStr.Split(',').Contains(vStr);
            }
            if (value is int i)
            {
                return i > 0;
            }
            return string.IsNullOrWhiteSpace(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
