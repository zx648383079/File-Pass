using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ZoDream.FileTransfer.Network.Messages
{
    public abstract class StringMessage : IMessagePackStream, IMessageUnpackStream
    {
        abstract protected string ToStr();
        abstract protected void FromStr(string val);

        public void Pack(SocketClient socket)
        {
            socket.SendText(ToStr());
        }

        public byte[] Pack()
        {
            var buffer = Encoding.UTF8.GetBytes(ToStr());
            return SocketHub.RenderPack(buffer, BitConverter.GetBytes((long)buffer.Length));
        }

        public void Unpack(SocketClient socket)
        {
            FromStr(socket.ReceiveText())
        }

        public void Unpack(byte[] buffer)
        {
            // var length = BitConverter.ToInt64(buffer[..8], 0);
            FromStr(Encoding.UTF8.GetString(buffer[8..]));
        }
    }
}
