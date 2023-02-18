using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class NoneMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; }

        public MessageItem ConverterTo()
        {
            return new ActionMessageItem()
            {
                Content = "拍拍你",
                CreatedAt = DateTime.Now,
            };
        }

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
