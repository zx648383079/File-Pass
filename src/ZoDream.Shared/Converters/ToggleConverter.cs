using System;
using System.Linq;

namespace ZoDream.Shared.Converters
{
    public static class ToggleConverter
    {
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
