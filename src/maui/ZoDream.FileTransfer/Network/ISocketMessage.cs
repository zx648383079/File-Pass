using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public interface ISocketMessage
    {
        public Task<bool> ReceiveAsync(SocketClient socket);

        public MessageItem ConverterTo();

        public Task<bool> SendAsync(SocketClient socket);
    }
}
