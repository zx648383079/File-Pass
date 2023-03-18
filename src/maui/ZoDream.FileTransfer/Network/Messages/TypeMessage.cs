using System.Net.Sockets;

namespace ZoDream.FileTransfer.Network.Messages
{
    public class TypeMessage : IMessagePackStream, IMessageUnpackStream
    {
        public SocketMessageType EventType { get; set; }

        public bool IsRequest { get; set; }

        public IMessagePack? SendData { get; set; }

        public IMessageUnpack? ReceiveData { get; set; }

        public void Pack(SocketClient socket)
        {
            socket.Send(EventType, IsRequest, SendData);
        }

        public byte[] Pack()
        {
            return Pack(EventType, IsRequest, SendData);
        }

        public void Unpack(SocketClient socket)
        {
            EventType = socket.ReceiveMessageType();
            if (MessageEventArg.HasRequest(EventType))
            {
                IsRequest = socket.ReceiveBool();
            }
            ReceiveData = MessageEventArg.RenderUnpack(EventType);
            if (ReceiveData is IMessageUnpackStream o)
            {
                o.Unpack(socket);
            }
            else
            {
                ReceiveData?.Unpack(socket.ReceiveBuffer());
            }

        }

        public void Unpack(byte[] buffer)
        {
            EventType = (SocketMessageType)buffer[0];
            var start = 1;
            if (MessageEventArg.HasRequest(EventType))
            {
                start = 2;
                IsRequest = Convert.ToBoolean(buffer[1]);
            }
            ReceiveData = MessageEventArg.RenderUnpack(EventType);
            ReceiveData?.Unpack(buffer[start..]);
        }

        public static byte[] Pack(SocketMessageType type, bool isRequest, IMessagePack? data)
        {
            var typeByte = (byte)type;
            var hasRequest = MessageEventArg.HasRequest(type);
            if (data == null)
            {
                return hasRequest ? new byte[] { typeByte, Convert.ToByte(isRequest) } : new byte[] { typeByte };
            }
            if (hasRequest)
            {
                return SocketHub.RenderPack(data.Pack(), typeByte, Convert.ToByte(isRequest));
            }
            return SocketHub.RenderPack(data.Pack(), typeByte);
        }
    }
}
