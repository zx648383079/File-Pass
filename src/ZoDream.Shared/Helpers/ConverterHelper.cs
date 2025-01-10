using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZoDream.Shared.Helpers
{
    public static class ConverterHelper
    {
        public static string FormatSize(long size)
        {
            var len = size.ToString().Length;
            if (len < 4)
            {
                return $"{size}B";
            }
            if (len < 7)
            {
                return Math.Round(Convert.ToDouble(size / 1024d), 2) + "KB";
            }
            if (len < 10)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024), 2) + "MB";
            }
            if (len < 13)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024), 2) + "GB";
            }
            if (len < 16)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024), 2) + "TB";
            }
            return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024 / 1024), 2) + "PB";
        }

        public static string FormatSize(object value)
        {
            if (value is not  null && long.TryParse(value.ToString(), out var size))
            {
                return FormatSize(size);
            }
            return "OB";
        }

        public static string FormatAgo(object value)
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
                return TimeHelper.FormatAgo(da);
            }
            var str = value.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return "--";
            }
            if (Regex.IsMatch(str, @"^\d+$"))
            {
                return TimeHelper.FormatAgo(str.Length > 10 ? int.Parse(str) / 1000 : int.Parse(str));
            }
            if (!DateTime.TryParse(str, out DateTime date))
            {
                return "--";
            }
            return TimeHelper.FormatAgo(date);
        }

        public static bool IsVisible(object value, object parameter)
        {
            if (value is null)
            {
                return false;
            }
            if (parameter is null)
            {
                if (value is int i)
                {
                    return i > 0;
                }
                return string.IsNullOrWhiteSpace(value.ToString());
            }
            var pStr = parameter.ToString();
            var vStr = value.ToString();
            if (pStr == vStr)
            {
                return true;
            }
            if (vStr is null || pStr is null)
            {
                return false;
            }
            var isRevert = false;
            if (pStr.StartsWith('^'))
            {
                isRevert = true;
                pStr = pStr[1..];
            }
            return pStr.Split('|').Contains(vStr) == !isRevert;
        }
    }
}
