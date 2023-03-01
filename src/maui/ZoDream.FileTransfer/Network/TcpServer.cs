using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class TcpServer: ISocketServer
    {
        public TcpServer(
            SocketHub hub)
        {
            Hub = hub;
        }

        private SocketHub Hub;
        private Socket ListenSocket;
        private string ListenIp = string.Empty;
        private int ListenPort = 0;
        private CancellationTokenSource ListenToken = new();
        public event MessageReceivedEventHandler MessageReceived;

        public void Listen(string ip, int port)
        {
            if (ListenIp == ip && ListenPort == port)
            {
                return;
            }
            var serverIp = new IPEndPoint(IPAddress.Parse(ip), port);
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

        public void Send(string ip, int port)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            ListenSocket?.Close();
        }
    }
}
