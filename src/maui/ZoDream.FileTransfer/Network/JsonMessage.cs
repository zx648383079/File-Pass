using System.Text.Json;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class JSONMessage<T> : ISocketMessage
    {
        public SocketMessageType Type { get; set; }

        public T Data { get; set; }


        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            var text = socket.ReceiveText();
            var res = JsonSerializer.Deserialize(text, typeof(T));
            if (res != null)
            {
                Data = (T)res;
            }
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.SendText(Type, JsonSerializer.Serialize(Data));
            return Task.FromResult(true);
        }
    }
}
