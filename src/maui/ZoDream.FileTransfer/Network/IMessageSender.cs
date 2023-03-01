using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public interface IMessageSender
    {

        public Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack pack);
        public Task<bool> RequestAsync(string ip, int port, SocketMessageType type, IMessagePack pack);
        public Task<bool> ResponseAsync(string ip, int port, SocketMessageType type, IMessagePack pack);
    }
}
