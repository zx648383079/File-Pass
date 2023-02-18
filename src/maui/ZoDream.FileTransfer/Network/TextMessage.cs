using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class TextMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; }

        public string Text { get; set; } = string.Empty;

        public MessageItem ConverterTo()
        {
            return new TextMessageItem()
            {
                Content = Text,
                CreatedAt = DateTime.Now,
            };
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Text = socket.ReceiveText();
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.SendText(Type, Text);
            return Task.FromResult(true);
        }
    }
}
