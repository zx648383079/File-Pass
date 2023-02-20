using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class NoneMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; }


        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.Send(Type);
            return Task.FromResult(true);
        }
    }
}
