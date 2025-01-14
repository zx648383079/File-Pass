﻿
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Converters
{
    internal class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((int)value > 99)
            {
                return "99+";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
