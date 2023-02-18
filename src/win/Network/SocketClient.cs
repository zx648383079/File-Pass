using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;

        private readonly Socket ClientSocket;
        private readonly CancellationTokenSource CancellationToken = new();

        public SocketClient(Socket socket)
        {
            ClientSocket = socket;
            LoopRev();
        }

        private void LoopRev()
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

                }
            }, CancellationToken.Token);
        }

        #region 接受消息
        private SocketMessageType ReceiveMessageType()
        {
            var buffer = new byte[1];
            ClientSocket.Receive(buffer);
            return (SocketMessageType)buffer[0];
        }

        private long ReceiveContentLength()
        {
            var buffer = new byte[8];
            ClientSocket.Receive(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        private string ReceiveText()
        {
            var length = ReceiveContentLength();
            var buffer = new byte[length];
            ClientSocket.Receive(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        private string ReceiveIp()
        {
            var text = ReceiveText();
            var arr = text.Split(':');
            Ip = arr[0];
            Port = arr.Length > 1 ? int.Parse(arr[1]) : 80;
            return Ip;
        }
        #endregion

        #region 发送消息

        private void Send(byte[] buffer)
        {
            if (!ClientSocket.Connected)
            {
                return;
            }
            ClientSocket.Send(buffer);
        }
        private void Send(long length)
        {
            Send(BitConverter.GetBytes(length));
        }

        private void Send(byte val)
        {
            Send(new byte[] { val});
        }

        private void Send(SocketMessageType messageType)
        {
            Send((byte)messageType);
        }

        public void SendText(SocketMessageType messageType, string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            Send(messageType);
            Send(buffer.Length);
            Send(buffer);
        }
        #endregion

        public void Dispose()
        {
            CancellationToken.Cancel();
            ClientSocket?.Close();
        }
    }
}
