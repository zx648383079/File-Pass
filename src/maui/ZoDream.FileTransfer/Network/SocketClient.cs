using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;

        private bool IsLoopReceive = false;
        private readonly Socket ClientSocket;
        private readonly CancellationTokenSource CancellationToken = new();
        public event MessageReceivedEventHandler MessageReceived;

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
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        IsLoopReceive = false;
                        return;
                    }
                    if (!ClientSocket.Connected)
                    {
                        IsLoopReceive = false;
                        MessageReceived?.Invoke(this, new NoneMessage() { Type = SocketMessageType.Close});
                        return;
                    }
                    var message = ReceiveAsync().GetAwaiter().GetResult();
                    if (message == null)
                    {
                        continue;
                    }
                    MessageReceived?.Invoke(this, message);
                }
            }, CancellationToken.Token);
        }

        #region 接受消息


        public async Task<ISocketMessage> ReceiveAsync()
        {
            if (!ClientSocket.Connected)
            {
                return new NoneMessage() { Type = SocketMessageType.Close };
            }
            var type = ReceiveMessageType();
            ISocketMessage message = type switch
            {
                SocketMessageType.Ip => new IpMessage(),
                SocketMessageType.CallFile or SocketMessageType.Numeric or SocketMessageType.String => new TextMessage(),
                SocketMessageType.AddUser or SocketMessageType.Bool => new BoolMessage(),
                SocketMessageType.Null or SocketMessageType.Close or SocketMessageType.Ping or SocketMessageType.CallInfo => new NoneMessage(),
                SocketMessageType.Info or SocketMessageType.CallAddUser => new JSONMessage<UserInfoItem>(),
                SocketMessageType.FileInfo => new JSONMessage<FileInfoItem>(),
                SocketMessageType.FilePart => new FilePartMessage(),// [titleLength:4][title][contentLength:4][stream]
                SocketMessageType.FileMerge => new FileMergeMessage(),// [md5Length][md5][partLength:4][p1,p2,p3]
                SocketMessageType.File => new FileMessage(),// [md5Length][md5][fileLength:4][stream]
                _ => new TextMessage(),
            };
            if (message == null)
            {
                return null;
            }
            message.Type = type;
            await message.ReceiveAsync(this);
            return message;
        }

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
        public async Task<ISocketMessage> ReceiveAsync(SocketMessageType messageType)
        {
            if (IsLoopReceive)
            {
                // 处于循环中无法获取
                return null;
            }
            while (true)
            {
                var message = await ReceiveAsync();
                if (message == null)
                {
                    return null;
                }
                MessageReceived?.Invoke(this, message);
                if (message.Type == messageType)
                {
                    return message;
                }
                if (!ClientSocket.Connected || CancellationToken.IsCancellationRequested)
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

        public void Send(byte val)
        {
            Send(new byte[] { val});
        }

        public void Send(SocketMessageType messageType)
        {
            Send((byte)messageType);
        }

        public async Task<bool> SendAsync(ISocketMessage message)
        {
            if (!ClientSocket.Connected || CancellationToken.IsCancellationRequested)
            {
                return false;
            }
            return await message.SendAsync(this);
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
                var buffer = new byte[Math.Min(rate, chunkSize)];
                reader.Read(buffer, 0, buffer.Length);
                ClientSocket.Send(buffer);
                rate -= buffer.Length;
            }
        }
        #endregion

        public void Dispose()
        {
            CancellationToken.Cancel();
            ClientSocket?.Close();
        }
    }
}
