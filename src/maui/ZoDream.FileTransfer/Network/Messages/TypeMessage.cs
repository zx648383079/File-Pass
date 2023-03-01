namespace ZoDream.FileTransfer.Network.Messages
{
    public class TypeMessage : IMessagePackStream, IMessageUnpackStream
    {
        public SocketMessageType EventType { get; set; }

        public bool IsRequest { get; set; }

        public IMessagePack SendData { get; set; }

        public IMessageUnpack ReceiveData { get; set; }

        public void Pack(SocketClient socket)
        {
            socket.Send(EventType);
            socket.Send(IsRequest);
            socket.Send(SendData);
        }

        public byte[] Pack()
        {
            return SocketHub.RenderPack(SendData.Pack(), (byte)EventType, Convert.ToByte(IsRequest));
        }

        public void Unpack(SocketClient socket)
        {
            EventType = socket.ReceiveMessageType();
            IsRequest = socket.ReceiveBool();
            ReceiveData = SocketHub.RenderUnpack(EventType);
            if (ReceiveData is IMessageUnpackStream o)
            {
                o.Unpack(socket);
            }
            else
            {
                ReceiveData.Unpack(socket.ReceiveBuffer());
            }

        }

        public void Unpack(byte[] buffer)
        {
            EventType = (SocketMessageType)buffer[0];
            IsRequest = Convert.ToBoolean(buffer[1]);
            ReceiveData = SocketHub.RenderUnpack(EventType);
            ReceiveData.Unpack(buffer[2..]);
        }
    }
}
