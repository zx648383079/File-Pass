using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ZoDream.Shared.Net
{
    public class TcpServer: ISocketServer
    {
        public TcpServer(
            SocketHub hub)
        {
            Hub = hub;
        }

        private readonly SocketHub Hub;
        private Socket? ListenSocket;
        private string ListenIp = string.Empty;
        private int ListenPort = 0;
        private CancellationTokenSource ListenToken = new();

        public bool IsListening => ListenSocket is not null && !ListenToken.IsCancellationRequested;

        public void Listen(string ip, int port)
        {
            if (ListenIp == ip && ListenPort == port)
            {
                return;
            }
            if (!IPAddress.TryParse(ip, out var address))
            {
                return;
            }
            var serverIp = new IPEndPoint(address, port);
            var tcpSocket = new Socket(serverIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                tcpSocket.Bind(serverIp);
            }
            catch (Exception)
            {
                return;
            }
            ListenToken?.Cancel();
            ListenSocket?.Close();
            ListenSocket = tcpSocket;
            ListenIp = ip;
            ListenPort = port;
            ListenToken = new CancellationTokenSource();
            var token = ListenToken.Token;
            Task.Factory.StartNew(() => {
                tcpSocket.Listen(10);
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    var client = tcpSocket.Accept();
                    Hub.Add(new SocketClient(client));
                }
            }, token);
        }

        public async Task<bool> SendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            var client = await Hub.GetAsync(address);
            if (client == null)
            {
                return false;
            }
            return client.Send(type, isRequest, pack);
        }

        public async Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            var client = await Hub.GetAsync(ip, port);
            if (client == null)
            {
                return false;
            }
            return client.Send(type, isRequest, pack);
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            ListenSocket?.Close();
        }
    }
}
