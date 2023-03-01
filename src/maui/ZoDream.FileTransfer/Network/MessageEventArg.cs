using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class MessageEventArg
    {

        public SocketMessageType EventType { get; private set; }

        public IMessageUnpack Data { get; private set; }

        public bool IsRequest { get; private set; }

        public MessageEventArg(SocketMessageType type, IMessageUnpack data):
            this(type, true, data)
        {
        }

        public MessageEventArg(SocketMessageType type, bool isRequest, IMessageUnpack data)
        {
            EventType = type;
            Data = data;
            IsRequest = isRequest;
        }
    }
}
