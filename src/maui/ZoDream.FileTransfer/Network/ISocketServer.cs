using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public interface ISocketServer: IDisposable
    {
        public bool IsListening { get; }
        public void Listen(string ip, int port);

        public Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack);

    }
}
