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
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (!ClientSocket.Connected)
                    {
                        return;
                    }
                    var message = ReceiveAsync().GetAwaiter().GetResult();
                    MessageReceived?.Invoke(Ip, message);
                }
            }, CancellationToken.Token);
        }

        #region 接受消息


        public async Task<ISocketMessage> ReceiveAsync()
        {
            var type = ReceiveMessageType();
            ISocketMessage message;
            switch (type)
            {
                case SocketMessageType.Ip:
                    message = new IpMessage();
                    break;
                case SocketMessageType.CallFile:
                case SocketMessageType.Numeric:
                case SocketMessageType.String:
                    message = new TextMessage() { Type = type };
                    break;
                case SocketMessageType.AddUser:
                case SocketMessageType.Bool:
                    message = new BoolMessage() { Type = type};
                    break;
                case SocketMessageType.Null:
                case SocketMessageType.Close:
                case SocketMessageType.Ping:
                case SocketMessageType.CallInfo:
                case SocketMessageType.CallAddUser:
                    message = new NoneMessage() { Type = type };
                    break;
                case SocketMessageType.Info:
                    message = new JSONMessage<UserInfoItem>() { Type = type};
                    break;
                case SocketMessageType.FileInfo:
                    message = new JSONMessage<FileInfoItem>() { Type = type };
                    break;
                case SocketMessageType.FilePart:
                    // [titleLength:4][title][contentLength:4][stream]
                    message = new FilePartMessage();
                    break;
                case SocketMessageType.FileMerge:
                    // [md5Length][md5][partLength:4][p1,p2,p3]
                    message = new FileMergeMessage();
                    break;
                case SocketMessageType.File:
                    // [md5Length][md5][fileLength:4][stream]
                    message = new FileMessage();
                    break;
                default:
                    message = new TextMessage() { Type = type };
                    break;
            }
            if (message == null)
            {
                return null;
            }
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
