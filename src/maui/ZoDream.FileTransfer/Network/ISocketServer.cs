using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public interface ISocketServer: IDisposable
    {
        public void Listen(string ip, int port);

        public void Send(string ip, int port);

    }
}
