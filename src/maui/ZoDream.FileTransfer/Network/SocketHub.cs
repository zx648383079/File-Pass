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

        private Socket ListenSocket;
        private string ListenIp = string.Empty;
        private int ListenPort = 0;
        private CancellationTokenSource ListenToken = new();
        private readonly IList<SocketClient> ClientItems = new List<SocketClient>();
        public event MessageReceivedEventHandler MessageReceived;

        public void Listen(string ip, int port)
        {
            if (ListenIp == ip &&  ListenPort == port)
            {
                return;
            }
            var serverIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(serverIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(serverIp);
            }
            catch (Exception)
            {
                return;
            }
            ListenToken?.Cancel();
            ListenSocket?.Close();
            ListenSocket = socket;
            ListenIp = ip;
            ListenPort = port;
            ListenToken = new CancellationTokenSource();
            var token = ListenToken.Token;
            Task.Factory.StartNew(() =>
            {
                socket.Listen(10);
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    var client = socket.Accept();
                    Add(new SocketClient(client));
                }
            }, token);
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

        private void Client_MessageReceived(SocketClient client, ISocketMessage message)
        {
            MessageReceived?.Invoke(client, message);
            if (message.Type == SocketMessageType.Close)
            {
                ClientItems.Remove(client);
                client?.Dispose();
            }
        }

        public SocketClient Connect(string ip, int port)
        {
            var clientIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(clientIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(clientIp);
            } catch (Exception)
            {
                return null;
            }
            if (!socket.Connected)
            {
                return null;
            }
            return new SocketClient(socket, ip, port);
        }

        public async Task<SocketClient> GetAsync(IUser user)
        {
            foreach (var item in ClientItems)
            {
                if (item.Ip != user.Ip || item.Port != user.Port)
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

        public async Task<bool> SendAsync(IUser user, ISocketMessage message)
        {
            return await message.SendAsync(await GetAsync(user)); 
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            foreach (var item in ClientItems)
            {
                item.Dispose();
            }
            ListenSocket?.Close();
        }
    }
}
