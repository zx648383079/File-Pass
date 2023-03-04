using System.Net.Sockets;
using System.Text;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network.Messages;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;
        public SocketHub? Hub { get; set; }
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

        public bool Connected => ClientSocket.Connected;

        public IClientAddress Address 
        {
            get {
                return new ClientAddress(Ip, Port);
            }
            set {
                Ip = value.Ip;
                Port = value.Port;
            }
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

        public void StopLoopReceive()
        {
            ReceiveToken?.Cancel();
            ReceiveToken = new CancellationTokenSource();
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
        public IMessageUnpack? Receive(SocketMessageType messageType)
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

        public bool SendIp(IClientAddress address)
        {
            if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
            {
                return false;
            }
            Send(SocketMessageType.Ip);
            new IpMessage() { 
                Ip = address.Ip,
                Port = address.Port,
            }.Pack(this);
            return true;
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
                Send(length);
                SendStream(reader, length);
                onProgress?.Invoke(length, length);
                return true;
            }
            var rate = length;
            var partItems = new List<string>();
            var i = 0;
            while (rate > 0)
            {
                if (!ClientSocket.Connected || token.IsCancellationRequested || SendToken.IsCancellationRequested)
                {
                    return false;
                }
                var partName = $"{md5}_{i}";
                Send(SocketMessageType.FilePart);
                SendText(partName);
                var partLength = Math.Min(rate, chunkSize);
                Send(partLength);
                SendStream(reader, partLength);
                partItems.Add(partName);
                rate -= chunkSize;
                i++;
                onProgress?.Invoke(Math.Min(length - rate, length), length);
            }
            Send(SocketMessageType.FileMerge);
            SendText(name);
            SendText(md5);
            SendText(string.Join(',', partItems));
            return true;
        }


        public string ReceiveFile(string folder, 
            Action<long, long> onProgress, 
            CancellationToken token = default)
        {
            var fileName = string.Empty;
            var md5 = string.Empty;
            var length = 0L;
            var location = string.Empty;
            var storage = App.Repository.Storage;
            while (true)
            {
                if (!ClientSocket.Connected || token.IsCancellationRequested 
                    || SendToken.IsCancellationRequested)
                {
                    return string.Empty;
                }
                var type = ReceiveMessageType();
                if (type == SocketMessageType.File)
                {
                    fileName = ReceiveText();
                    md5 = ReceiveText();
                    length = ReceiveContentLength();
                    location = Path.Combine(folder, fileName);
                    using (var fs = storage.CacheWriter(md5))
                    {
                        ReceiveStream(fs, length);
                    }
                    onProgress?.Invoke(length, length);
                    if (md5 != storage.CacheFileMD5(md5))
                    {
                        storage.CacheRemove(md5);
                        return string.Empty;
                    }
                    storage.CacheMove(md5, location);
                    
                    return fileName;
                } else if (type == SocketMessageType.FileMerge)
                {
                    fileName = ReceiveText();
                    location = Path.Combine(folder, fileName);
                    md5 = ReceiveText();
                    var partItems = ReceiveText().Split(',');
                    var partLength = storage.CacheMergeFile(md5, partItems);
                    if (partLength <= 0
                        || md5 != storage.CacheFileMD5(md5))
                    {
                        storage.CacheRemove(partItems);
                        storage.CacheRemove(md5);
                        return string.Empty;
                    }
                    storage.CacheRemove(partItems);
                    storage.CacheMove(md5, location);
                    onProgress?.Invoke(length, length);
                    return fileName;
                } else if (type == SocketMessageType.FilePart)
                {
                    var partName = ReceiveText();
                    var partLength = ReceiveContentLength();
                    using (var fs = storage.CacheWriter(partName))
                    {
                        ReceiveStream(fs, partLength);
                    }
                    length += partLength;
                    onProgress?.Invoke(length, 0L);
                } else
                {
                    App.Repository.Logger.Warning($"File Receive Unknown Type:{type}");
                    return string.Empty;
                }
            }
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
