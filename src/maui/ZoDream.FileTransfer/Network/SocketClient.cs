using System.Net.Sockets;
using System.Text;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;
        public SocketHub Hub { get; set; }
        private bool IsLoopReceive = false;
        private readonly Socket ClientSocket;
        private CancellationTokenSource ReceiveToken = new();
        private CancellationTokenSource SendToken = new();

        public SocketClient(Socket socket)
        {
            ClientSocket = socket;
        }

        public SocketClient(Socket socket, string ip, int port): this(socket)
        {
            Ip = ip;
            Port = port;
        }

        public void LoopReceive()
        {
            IsLoopReceive = true;
            ReceiveToken?.Cancel();
            ReceiveToken = new CancellationTokenSource();
            var token = ReceiveToken.Token;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        IsLoopReceive = false;
                        return;
                    }
                    if (!ClientSocket.Connected)
                    {
                        IsLoopReceive = false;
                        Hub?.Close(this);
                        return;
                    }
                    Hub?.Emit(this);
                }
            }, token);
        }

        #region 接受消息

        public SocketMessageType ReceiveMessageType()
        {
            var buffer = new byte[1];
            ClientSocket.Receive(buffer);
            return (SocketMessageType)buffer[0];
        }

        public long ReceiveContentLength()
        {
            var buffer = new byte[8];
            ClientSocket.Receive(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public string ReceiveText()
        {
            var length = ReceiveContentLength();
            return ReceiveText(length);
        }

        public byte[] ReceiveBuffer()
        {
            var length = ReceiveContentLength();
            var buffer = new byte[length];
            ClientSocket.Receive(buffer);
            return buffer;
        }

        public void ReceiveStream(Stream writer,long length)
        {
            var chunkSize = 400;
            var rate = length;
            while (rate > 0)
            {
                var buffer = new byte[Math.Min(rate, chunkSize)];
                ClientSocket.Receive(buffer);
                writer.Write(buffer, 0, buffer.Length);
                rate -= buffer.Length;
            }
        }

        public string ReceiveText(long length)
        {
            var buffer = new byte[length];
            ClientSocket.Receive(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        public bool ReceiveBool()
        {
            var buffer = new byte[1];
            ClientSocket.Receive(buffer);
            return Convert.ToBoolean(buffer[0]);
        }

        /// <summary>
        /// 等待获取下一条信息
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public async Task<IMessageUnpack> ReceiveAsync(SocketMessageType messageType)
        {
            if (IsLoopReceive)
            {
                // 处于循环中无法获取
                return null;
            }
            while (true)
            {
                var message = Hub?.Emit(this);
                if (message == null)
                {
                    return null;
                }
                if (message.EventType == messageType)
                {
                    return message.Data;
                }
                if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
                {
                    return null;
                }
            }
        }

        #endregion

        #region 发送消息

        public void Send(byte[] buffer)
        {
            if (!ClientSocket.Connected)
            {
                return;
            }
            ClientSocket.Send(buffer);
        }
        public void Send(long length)
        {
            Send(BitConverter.GetBytes(length));
        }

        public void Send(bool val)
        {
            Send(Convert.ToByte(val));
        }

        public void Send(byte val)
        {
            Send(new byte[] { val});
        }

        public void Send(SocketMessageType messageType)
        {
            Send((byte)messageType);
        }

        public bool Send(IMessagePack message)
        {
            if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
            {
                return false;
            }
            if (message is IMessagePackStream o)
            {
                o.Pack(this);
            } else
            {
                Send(message.Pack());
            }
            return true;
        }

        public void SendText(SocketMessageType messageType, string text)
        {
            Send(messageType);
            SendText(text);
        }

        public void SendText(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            Send(buffer.Length);
            Send(buffer);
        }

        public void SendStream(Stream reader, long length)
        {
            var chunkSize = 400;
            var rate = length;
            while (rate > 0)
            {
                if (SendToken.IsCancellationRequested)
                {
                    return;
                }
                var buffer = new byte[Math.Min(rate, chunkSize)];
                reader.Read(buffer, 0, buffer.Length);
                ClientSocket.Send(buffer);
                rate -= buffer.Length;
            }
        }

        public bool SendFile(string name, 
            string md5, 
            string fileName, 
            Action<long, long> onProgress = null,
            CancellationToken token = default)
        {
            var chunkSize = 2000000;
            using var reader = File.OpenRead(fileName);
            var length = reader.Length;
            if (length <= chunkSize)
            {
                Send(SocketMessageType.File);
                SendText(name);
                SendText(md5);
                SendStream(reader, length);
                onProgress?.Invoke(length, length);
                return true;
            }
            var rate = length;
            var partItems = new List<string>();
            var i = 0;
            while (rate > 0)
            {
                if (token.IsCancellationRequested || SendToken.IsCancellationRequested)
                {
                    return false;
                }
                var partName = $"{md5}_{i}";
                Send(SocketMessageType.FilePart);
                SendText(partName);
                SendStream(reader, Math.Min(rate, chunkSize));
                partItems.Add(partName);
                rate -= chunkSize;
                i++;
                onProgress?.Invoke(Math.Min(length - rate, length), length);
            }
            Send(SocketMessageType.FileMerge);
            SendText($"{md5}_{i}");
            SendText(string.Join(",", partItems));
            SendText(name);
            return true;
        }
        #endregion

        public void Dispose()
        {
            ReceiveToken?.Cancel();
            SendToken?.Cancel();
            ClientSocket?.Close();
        }
    }
}
