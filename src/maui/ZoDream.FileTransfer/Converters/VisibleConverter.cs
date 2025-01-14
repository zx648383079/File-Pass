﻿using Microsoft.Maui.Controls;
using System;
using System.Globalization;
using ZoDream.Shared.Helpers;

namespace ZoDream.FileTransfer.Converters
{
    internal class VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConverterHelper.IsVisible(value, parameter);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
