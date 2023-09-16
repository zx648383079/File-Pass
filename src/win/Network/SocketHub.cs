using NetFwTypeLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Loggers;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    /// <summary>
    /// 消息传递类
    /// </summary>
    public class SocketHub : IDisposable
    {
        public const string Version = "1.0.1";
        public int ThreadCount { get; set; } = 5;
        private string ListenIp = string.Empty;
        private int ListenPort = 0;
        private Socket? ListenSocket;
        private CancellationTokenSource ListenToken = new();
        private CancellationTokenSource SendToken = new();
        private bool IsSending = false;
        private readonly List<SocketClient> LinkItems = new();
        private readonly ConcurrentDictionary<string, FileMessageSocket> FileItems = new();

        public SocketHub(ILogger logger)
        {
            Logger = logger;
        }

        public string WorkFolder { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = true;
        public ILogger Logger { get; private set; }

        public int LinkedCount => LinkItems.Count;

        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;
        public event LinkChangeEventHandler? OnLinkChange;

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
            NetFwAddPorts("LargeFile", port, "TCP");
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
            Task.Factory.StartNew(() =>
            {
                tcpSocket.Listen(10);
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    var socket = tcpSocket.Accept();
                    Add(new SocketClient(socket));
                }
            }, token);
        }

        public static SocketClient? Connect(string ip, int port)
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                return null;
            }
            var clientIp = new IPEndPoint(address, port);
            var socket = new Socket(clientIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(clientIp);
            }
            catch (Exception)
            {
                // 可能对方没有开启 tcp
                return null;
            }
            if (!socket.Connected)
            {
                return null;
            }
            var link = new SocketClient(socket, ip, port);
            link.SendText(SocketMessageType.Header, Version);
            return link;
        }

        public SocketClient? Add(string ip, int port)
        {
            var client = Connect(ip, port);
            if (client == null)
            {
                return null;
            }
            Add(client, false);
            return client;
        }

        public SocketClient? Add(string ip, int port, bool isReceive)
        {
            var client = Connect(ip, port);
            if (client == null)
            {
                return null;
            }
            Add(client, isReceive);
            if (isReceive)
            {
                client.AsReceiveLink();
            }
            return client;
        }

        public async Task<SocketClient?> GetAsync(string ip, int port)
        {
            foreach (var item in LinkItems)
            {
                if (item.Ip != ip || item.Port != port)
                {
                    continue;
                }
                if (!item.Connected)
                {
                    continue;
                }
                return item;
            }
            return await Task.Factory.StartNew(() => {
                var client = Connect(ip, port);
                if (client == null)
                {
                    return null;
                }
                Add(client, false);
                return client;
            });
        }

        public void Add(SocketClient client)
        {
            if (client == null)
            {
                return;
            }
            client.Hub = this;
            LinkItems.Add(client);
            client.LoopReceive();
            OnLinkChange?.Invoke();
        }

        public void Add(SocketClient client, bool isReceive)
        {
            if (client == null)
            {
                return;
            }
            client.Hub = this;
            if (!isReceive)
            {
                LinkItems.Add(client);
                OnLinkChange?.Invoke();
                return;
            }
            LinkItems.Add(client);
            client.LoopReceive();
            OnLinkChange?.Invoke();
        }

        #region 主动模式下发送文件
        public FileMessageSocket GetFilePack(string ip, int port)
        {
            if (FileItems.TryGetValue(ip, out FileMessageSocket? file))
            {
                file.Port = port;
                return file;
            }
            file = new FileMessageSocket(this, ip, port);
            FileItems.TryAdd(ip, file);
            return file;
        }

        private void SendFile()
        {
            if (IsSending)
            {
                return;
            }
            IsSending = true;
            var token = SendToken.Token;
            Task.Factory.StartNew(() => {
                while (true)
                {
                    Thread.Sleep(500);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    var max = ThreadCount;
                    var rate = max - LinkItems.Count;
                    if (rate <= 0)
                    {
                        continue;
                    }
                    foreach (var item in FileItems)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        rate -= item.Value.UseThread(rate, token);
                        if (rate <= 0)
                        {
                            break;
                        }
                    }
                    if (rate == max)
                    {
                        IsSending = false;
                        return;
                    }
                }
            }, token);
        }

        public Task<bool> SendFileAsync(string ip, int port,
            string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(false);
            }
            GetFilePack(ip, port).Add(new FileInfoItem(fileInfo.Name, fileName, fileInfo.Name, fileInfo.Length));
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(string ip, int port, 
            string name, string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(false);
            }
            GetFilePack(ip, port).Add(new FileInfoItem(fileInfo.Name, 
                fileName, name, fileInfo.Length));
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(string ip, int port, 
            IEnumerable<FileInfoItem> items)
        {
            GetFilePack(ip, port).Add(items);
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(string ip, int port,
            FileInfoItem item)
        {
            GetFilePack(ip, port).Add(item);
            SendFile();
            return Task.FromResult(true);
        }

        #endregion

        #region 被动模式下
        /// <summary>
        /// 获取被动连接
        /// </summary>
        /// <returns></returns>
        public SocketClient? Get()
        {
            foreach (var item in LinkItems)
            {
                if (item.IsPassively && !item.IsBusy && item.IsReceiveLink)
                {
                    return item;
                }
            }
            return null;
        }

        public Task<bool> SendFileAsync(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(false);
            }
            GetFilePack(string.Empty, 80).Add(new FileInfoItem(fileInfo.Name, fileName, fileInfo.Name, fileInfo.Length));
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(string name, string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(false);
            }
            GetFilePack(string.Empty, 80).Add(new FileInfoItem(fileInfo.Name,
                fileName, name, fileInfo.Length));
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(IEnumerable<FileInfoItem> items)
        {
            GetFilePack(string.Empty, 80).Add(items);
            SendFile();
            return Task.FromResult(true);
        }

        public Task<bool> SendFileAsync(FileInfoItem item)
        {
            GetFilePack(string.Empty, 80).Add(item);
            SendFile();
            return Task.FromResult(true);
        }
        #endregion

        public void StopSend()
        {
            SendToken.Cancel();
            foreach (var item in FileItems)
            {
                item.Value.Dispose();
            }
            foreach (var item in LinkItems)
            {
                item.Dispose();
            }
            FileItems.Clear();
            LinkItems.Clear();
            SendToken = new CancellationTokenSource();
            IsSending = false;
        }

        public void EmitReceive(string name, string fileName, long progress, long total)
        {
            Emit(name, fileName, progress, total, false);
        }

        public void EmitSend(string name, string fileName, long progress, long total)
        {
            Emit(name, fileName, progress, total, true);
        }

        public void Emit(string name, string fileName, long progress, long total, bool isSend)
        {
            if (string.IsNullOrWhiteSpace(name) 
                || string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }
            OnProgress?.Invoke(name, fileName, progress, total, isSend);
            if (total == 0)
            {
                OnCompleted?.Invoke(name, fileName, false, isSend);
            }
            else if (total <= progress)
            {
                OnCompleted?.Invoke(name, fileName, true, isSend);
            }
        }

        public void Close(SocketClient client)
        {
            LinkItems.Remove(client);
            client.Dispose();
            OnLinkChange?.Invoke();
        }

        public void Dispose()
        {
            ListenToken.Cancel();
            SendToken?.Cancel();
            foreach (var item in LinkItems)
            {
                item.Dispose();
            }
            foreach (var item in FileItems)
            {
                item.Value.Dispose();
            }
            ListenSocket?.Close();
        }

        


        /// <summary>
        /// 添加防火墙例外端口
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="port">端口</param>
        /// <param name="protocol">协议(TCP、UDP)</param>
        public static void NetFwAddPorts(string name, int port, string protocol)
        {
            var mgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
            var openPortType = Type.GetTypeFromProgID("HNetCfg.FwOpenPort");
            if (mgrType == null || openPortType == null)
            {
                return;
            }
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            var netFwMgr = (INetFwMgr)Activator.CreateInstance(mgrType);
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            var objPort = (INetFwOpenPort)Activator.CreateInstance(openPortType);
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            if (objPort == null || netFwMgr == null)
            {
                return;
            }
            objPort.Name = name;
            objPort.Port = port;
            if (protocol.ToUpper() == "TCP")
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            }
            else
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
            }
            objPort.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            objPort.Enabled = true;

            var exist = false;
            foreach (INetFwOpenPort mPort in netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts)
            {
                if (objPort == mPort)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts.Add(objPort);
            }
        }

        
    }
}
