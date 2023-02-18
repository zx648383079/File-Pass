using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public enum SocketMessageType: byte
    {
        None = 0,
        Ip,
        String,
        Numeric,
        Bool,
        Null,
        Ping,
        File,
        Close,
    }
}
