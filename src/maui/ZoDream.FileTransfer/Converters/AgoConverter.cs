using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Converters
{
    public class AgoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "--";
            }
            if (value is DateTime da)
            {
                if (da == DateTime.MinValue)
                {
                    return string.Empty;
                }
                return Time.FormatAgo(da);
            }
            var str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return "--";
            }
            if (Regex.IsMatch(str, @"^\d+$"))
            {
                return Time.FormatAgo(str.Length > 10 ? int.Parse(str) / 1000 : int.Parse(str));
            }
            if (!DateTime.TryParse(str, out DateTime date))
            {
                return "--";
            }
            return Time.FormatAgo(date);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
