using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageProgressEventHandler(string name,
        string fileName, long progress, long total, bool isSend);
    public delegate void MessageCompletedEventHandler(string name,
        string fileName, bool isSuccess, bool isSend);
}
