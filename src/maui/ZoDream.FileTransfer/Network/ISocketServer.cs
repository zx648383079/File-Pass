using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public interface ISocketServer: IDisposable
    {
        public bool IsListening { get; }
        public void Listen(string ip, int port);

        public Task<bool> SendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack? pack);

    }
}
