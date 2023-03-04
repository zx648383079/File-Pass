﻿using System.Net.Sockets;
using System.Text;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network.Messages;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        private readonly int ChunkSize = 500;
        private readonly int FileChunkSize = 100 * 1024;
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

        private bool connected = true;
        public bool Connected => connected && ClientSocket.Connected;
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

        private byte[] Receive(int length)
        {
            if (length <= 0 || length > FileChunkSize)
            {
                return Array.Empty<byte>();
            }
            var buffer = new byte[length];
            try
            {
                var index = 0;
                while (index < length)
                {
                    var size = ClientSocket.Receive(buffer, index,
                        length - index, SocketFlags.None);
                    index += size;
                }
            }
            catch (Exception ex)
            {
                connected = false;
                Hub?.Logger.Error(ex.Message);
            }
            return buffer;
        }

        public SocketMessageType ReceiveMessageType()
        {
            var buffer = Receive(1);
            return (SocketMessageType)buffer[0];
        }

        public long ReceiveContentLength()
        {
            var buffer = Receive(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public string ReceiveText()
        {
            var length = ReceiveContentLength();
            return ReceiveText((int)length);
        }

        public byte[] ReceiveBuffer()
        {
            var length = ReceiveContentLength();
            return Receive((int)length);
        }

        public void ReceiveStream(Stream writer,long length)
        {
            var rate = length;
            while (rate > 0)
            {
                var buffer = Receive((int)Math.Min(rate, ChunkSize));
                if (buffer.Length < 1)
                {
                    return;
                }
                writer.Write(buffer, 0, buffer.Length);
                rate -= buffer.Length;
            }
        }

        public string ReceiveText(int length)
        {
            var buffer = Receive(length);
            return Encoding.UTF8.GetString(buffer);
        }

        public bool ReceiveBool()
        {
            var buffer = Receive(1);
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
                }
                else if (type == SocketMessageType.FileMerge)
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
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = ReceiveText();
                    var partLength = ReceiveContentLength();
                    using (var fs = storage.CacheWriter(partName))
                    {
                        ReceiveStream(fs, partLength);
                    }
                    length += partLength;
                    onProgress?.Invoke(length, 0L);
                }
                else
                {
                    App.Repository.Logger.Warning($"File Receive Unknown Type:{type}");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// 跳过
        /// </summary>
        public void Jump()
        {
            Jump(ReceiveContentLength());
        }

        /// <summary>
        /// 跳过指定字节
        /// </summary>
        /// <param name="length"></param>
        public void Jump(long length)
        {
            var rate = length;
            while (rate > 0)
            {
                var buffer = new byte[Math.Min(rate, ChunkSize)];
                ClientSocket.Receive(buffer);
                rate -= buffer.Length;
            }
        }
        #endregion

        #region 发送消息

        private void Send(byte[] buffer, int length)
        {
            if (!Connected)
            {
                return;
            }
            try
            {
                var index = 0;
                while (index < length)
                {
                    var size = ClientSocket.Send(buffer, index, length - index, SocketFlags.None);
                    index += size;
                }
            }
            catch (Exception ex)
            {
                connected = false;
                Hub?.Logger.Error(ex.Message);
            }
        }

        public void Send(byte[] buffer)
        {
            Send(buffer, buffer.Length);
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
            var sent = 0L;
            while (sent < length)
            {
                if (!ClientSocket.Connected || SendToken.IsCancellationRequested)
                {
                    return;
                }
                var size = (int)Math.Min(length - sent, ChunkSize);
                var buffer = new byte[size];
                size = reader.Read(buffer, 0, size);
                if (size != buffer.Length)
                {
                    Hub?.Logger.Error("长度不对");
                }
                try
                {
                    Send(buffer, size);
                }
                catch (Exception ex)
                {
                    connected = false;
                    Hub?.Logger.Error(ex.Message);
                    return;
                }
                sent += buffer.Length;
            }
        }

        public bool SendFile(string name, 
            string md5, 
            string fileName, 
            Action<long, long>? onProgress = null,
            CancellationToken token = default)
        {
            var chunkSize = FileChunkSize;
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


        #endregion

        public void Dispose()
        {
            ReceiveToken?.Cancel();
            SendToken?.Cancel();
            ClientSocket?.Close();
        }
    }
}
