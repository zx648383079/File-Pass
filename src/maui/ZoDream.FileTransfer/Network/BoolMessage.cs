using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class BoolMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; }

        public bool Value { get; set; }

        public MessageItem ConverterTo()
        {
            return new TextMessageItem()
            {
                Content = "",
                CreatedAt = DateTime.Now,
            };
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Value = socket.ReceiveBool();
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.Send(Type);
            socket.Send(Convert.ToByte(Value));
            return Task.FromResult(true);
        }
    }
}
