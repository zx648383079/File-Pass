using System;
using System.Globalization;
using System.Windows.Data;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Converters
{
    public class FileStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((FileStatus)value)
            {
                case FileStatus.ReadyReceive:
                    return "准备接收";
                case FileStatus.Receiving:
                    return "接收中";
                case FileStatus.Received:
                    return "接收成功";
                case FileStatus.ReceiveIgnore:
                    return "接收跳过";
                case FileStatus.ReceiveFailure:
                    return "接收失败";
                case FileStatus.ReadySend:
                    return "准备发送";
                case FileStatus.Sending:
                    return "发送中";
                case FileStatus.Sent:
                    return "发送成功";
                case FileStatus.SendIgnore:
                    return "发送跳过";
                case FileStatus.SendFailure:
                    return "发送失败";
                default:
                    return "-";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
