using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public interface IMessageSender
    {

        public Task<bool> SendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack pack);
        public Task<bool> RequestAsync(IClientAddress address, SocketMessageType type, IMessagePack? pack);
        public Task<bool> ResponseAsync(IClientAddress address, SocketMessageType type, IMessagePack? pack);

        public Task<bool> UdpSendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack? pack);
    }
}
