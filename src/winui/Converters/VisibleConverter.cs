﻿using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using ZoDream.Shared.Helpers;

namespace ZoDream.FileTransfer.Converters
{
    internal class VisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ConverterHelper.IsVisible(value, parameter);
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}