using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Network.Messages;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.Network
{
    public class UdpServer : ISocketServer
    {

        public UdpServer(
            SocketHub hub)
        {
            Hub = hub;
        }

        private readonly SocketHub Hub;
        private readonly byte[] CacheBuffer = new byte[Constants.UDP_BUFFER_SIZE];
        private Socket? ListenSocket;
        public string ListenIp { get; private set; } = string.Empty;
        public int ListenPort { get; private set; } = 0;
        /// <summary>
        /// 重试次数
        /// </summary>
        public int Retry = 5;
        private CancellationTokenSource ListenToken = new();

        public bool IsListening => ListenSocket is not null && !ListenToken.IsCancellationRequested;
        public void Listen(string ip, int port)
        {
            if (ListenIp == ip && ListenPort == port)
            {
                return;
            }
            var serverIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var udpSocket = new Socket(serverIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                udpSocket.Bind(serverIp);
            }
            catch (Exception ex)
            {
                Hub?.Logger.Error($"{ip}:{port}->{ex.Message}");
                return;
            }
            ListenToken?.Cancel();
            ListenSocket?.Close();

            ListenSocket = udpSocket;

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
                        var length = udpSocket.ReceiveFrom(CacheBuffer, Constants.UDP_BUFFER_SIZE,
                            SocketFlags.None, ref sendIp);
                        var buffer = new byte[length];
                        Buffer.BlockCopy(CacheBuffer, 0, buffer, 0, length);
                        if (sendIp is IPEndPoint o)
                        {
                            Hub?.Emit(o.Address.ToString(), o.Port, buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Repository.Logger.Error(ex.Message);
                    }
                }
            }, token);
        }

        public Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            var buffer = TypeMessage.Pack(type, isRequest, pack);
            if (buffer.Length > Constants.UDP_BUFFER_SIZE)
            {
                App.Repository.Logger.Error($"UDP Send Max Size: {Constants.UDP_BUFFER_SIZE}");
                return Task.FromResult(false);
            }
            return Task.FromResult(Send(ip, port, buffer));
        }

        public bool Ping(string ip, byte[] buffer)
        {
            return Ping(ip, ListenPort, buffer);
        }

        public bool Ping(string ip, int port, byte[] buffer)
        {
            return Send(ip, port, buffer);
        }

        public bool Send(string ip, int port, byte[] buffer)
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                Hub?.Logger.Error($"IP is error: {ip}");
                return false;
            }
            var remote = new IPEndPoint(address, port);
            try
            {
                var i = 0;
                while (i < Retry)
                {
                    var sent = ListenSocket?.SendTo(buffer, 0, buffer.Length, SocketFlags.None, remote);
                    if (sent >= buffer.Length)
                    {
                        return true;
                    }
                    i++;
                }
                
            }
            catch (Exception ex)
            {
                Hub?.Logger.Error(ex.Message);
                return false;
            }
            return false;
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            ListenSocket?.Close();
        }
    }
}
