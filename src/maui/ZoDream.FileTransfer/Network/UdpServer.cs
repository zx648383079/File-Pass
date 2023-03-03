using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class UdpServer : ISocketServer
    {

        public UdpServer(
            SocketHub hub)
        {
            Hub = hub;
        }

        private SocketHub Hub;
        private readonly byte[] CacheBuffer = new byte[65536];
        private Socket? ListenSocket;
        public string ListenIp { get; private set; } = string.Empty;
        public int ListenPort { get; private set; } = 0;
        private CancellationTokenSource ListenToken = new();

        public void Listen(string ip, int port)
        {
            if (ListenIp == ip && ListenPort == port)
            {
                return;
            }
            var serverIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var tcpSocket = new Socket(serverIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
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
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    try
                    {
                        EndPoint sendIp = new IPEndPoint(IPAddress.Any, port);
                        var length = tcpSocket.ReceiveFrom(CacheBuffer, 65536,
                            SocketFlags.None, ref sendIp);
                        var buffer = new byte[length];
                        Buffer.BlockCopy(CacheBuffer, 0, buffer, 0, length);
                        if (sendIp is IPEndPoint o)
                        {
                            Hub.Emit(o.Address.ToString(), o.Port, buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Repository.Logger.Error(ex.Message);
                    }
                }
            }, token);
        }

        public void Send(string ip, int port)
        {
            var remote = new IPEndPoint(IPAddress.Parse(ip), port);
            var buffer = new byte[1024];
            ListenSocket?.SendTo(buffer, remote);
        }

        public void Ping(string ip, byte[] buffer)
        {
            Ping(ip, ListenPort, buffer);
        }

        public void Ping(string ip, int port, byte[] buffer)
        {
            var remote = new IPEndPoint(IPAddress.Parse(ip), port);
            ListenSocket?.SendTo(buffer, remote);
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            ListenSocket?.Close();
        }
    }
}
