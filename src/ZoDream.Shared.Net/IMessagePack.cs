using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.Shared.Net
{
    public interface IMessagePack
    {

        public byte[] Pack();
    }

    public interface IMessageUnpack
    {

        public void Unpack(byte[] buffer);
    }

    public interface IMessagePackStream: IMessagePack
    {

        public void Pack(SocketClient socket);
    }

    public interface IMessageUnpackStream: IMessageUnpack
    {

        public void Unpack(SocketClient socket);
    }
}
