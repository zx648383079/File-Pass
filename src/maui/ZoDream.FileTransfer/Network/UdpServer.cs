﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
        private CancellationTokenSource ListenToken = new();

        public bool IsListening => ListenSocket is not null && !ListenToken.IsCancellationRequested;
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
                        var length = tcpSocket.ReceiveFrom(CacheBuffer, Constants.UDP_BUFFER_SIZE,
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

        public Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            byte[] buffer;
            if (pack is not null)
            {
                buffer = SocketHub.RenderPack(pack.Pack(), (byte)type, Convert.ToByte(isRequest));
            } else
            {
                buffer = new byte[] { (byte)type, Convert.ToByte(isRequest) };
            }
            if (buffer.Length > Constants.UDP_BUFFER_SIZE)
            {
                App.Repository.Logger.Error($"UDP Send Max Size: {Constants.UDP_BUFFER_SIZE}");
                return Task.FromResult(false);
            }
            Send(ip, port, buffer);
            return Task.FromResult(true);
        }

        public void Ping(string ip, byte[] buffer)
        {
            Ping(ip, ListenPort, buffer);
        }

        public void Ping(string ip, int port, byte[] buffer)
        {
            Send(ip, port, buffer);
        }

        public void Send(string ip, int port, byte[] buffer)
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
