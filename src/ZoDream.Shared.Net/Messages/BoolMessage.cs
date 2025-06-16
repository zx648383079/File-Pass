using System;

namespace ZoDream.Shared.Net
{
    public class BoolMessage : IMessagePackStream, IMessageUnpackStream
    {
        public bool Data { get; set; }

        public void Pack(SocketClient socket)
        {
            socket.Send(Convert.ToByte(Data));
        }

        public byte[] Pack()
        {
            return new byte[] { Convert.ToByte(Data) };
        }

        public void Unpack(SocketClient socket)
        {
            Data = socket.ReceiveBool();
        }

        public void Unpack(byte[] buffer)
        {
            Data = Convert.ToBoolean(buffer[0]);
        }
    }
}
