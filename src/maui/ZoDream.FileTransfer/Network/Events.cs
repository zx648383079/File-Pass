using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageReceivedEventHandler(string ip, ISocketMessage message);
}
