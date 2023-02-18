 using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    /// <summary>
    /// 消息传递类
    /// 消息格式 [1B:消息体类型][8B:消息体长度][-消息体内容]
    /// </summary>
    public class SocketHub : IDisposable
    {

        private Socket ServerSocket;
        private readonly CancellationTokenSource CancellationToken = new();
        private readonly IList<SocketClient> ClientItems = new List<SocketClient>();
        public event MessageReceivedEventHandler MessageReceived;

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
                    Add(new SocketClient(socket));
                }
            }, CancellationToken.Token);
        }

        public void Add(SocketClient client)
        {
            if (client == null)
            {
                return;
            }
            ClientItems.Add(client);
            client.LoopReceive();
            client.MessageReceived += Client_MessageReceived;
        }

        private void Client_MessageReceived(string ip, ISocketMessage message)
        {
            MessageReceived?.Invoke(ip, message);
        }

        public SocketClient Connect(string ip, int port)
        {
            var clientIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(clientIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(clientIp);
            if (!socket.Connected)
            {
                return null;
            }
            return new SocketClient(socket, ip, port);
        }

        public async Task<SocketClient> GetAsync(UserItem user)
        {
            foreach (var item in ClientItems)
            {
                if (item.Ip != user.Ip)
                {
                    continue;
                }
                return item;
            }
            return await Task.Factory.StartNew(() => 
            {
                var client = Connect(user.Ip, user.Port);
                if (client == null)
                {
                    return null;
                }
                Add(client);
                return client;
            });
        } 

        public async Task<bool> SendAsync(UserItem user, ISocketMessage message)
        {
            return await message.SendAsync(await GetAsync(user)); 
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
