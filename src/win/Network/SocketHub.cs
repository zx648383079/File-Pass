using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    /// <summary>
    /// 消息传递类
    /// 消息格式 [1B:消息体类型][8B:消息体长度][-消息体内容]
    /// </summary>
    public class SocketHub : IDisposable
    {

        private Socket? ServerSocket;
        private readonly CancellationTokenSource CancellationToken = new();
        private readonly IList<SocketClient> ClientItems = new List<SocketClient>();

        public void Listen(string ip, int port)
        {
            var serverIp = new IPEndPoint(IPAddress.Parse(ip), port);
            ServerSocket = new Socket(serverIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(serverIp);
            Task.Factory.StartNew(() =>
            {
                ServerSocket.Listen(10);
                while (true)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    var socket = ServerSocket.Accept();
                    ClientItems.Add(new SocketClient(socket));
                }
            }, CancellationToken.Token);
        }



        public void Dispose()
        {
            CancellationToken.Cancel();
            foreach (var item in ClientItems)
            {
                item.Dispose();
            }
            ServerSocket?.Close();
        }
    }
}
