using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using static System.Net.Mime.MediaTypeNames;

namespace ZoDream.FileTransfer.Network
{
    public class IpMessage : ISocketMessage
    {
        const string Separator = ":";
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;

        public MessageItem ConverterTo()
        {
            return new ActionMessageItem()
            {
                Content = "ip切换",
                CreatedAt = DateTime.Now,
            };
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            var text = socket.ReceiveText();
            var arr = text.Split(Separator);
            Ip = arr[0];
            Port = arr.Length > 1 ? int.Parse(arr[1]) : 80;
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.SendText(SocketMessageType.Ip, $"{Ip}{Separator}{Port}");
            return Task.FromResult(true);
        }
    }
}
