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
        public SocketMessageType Type { get; set; }

        public Task<bool> ReceiveAsync(SocketClient socket);

        public Task<bool> SendAsync(SocketClient socket);
    }
}
