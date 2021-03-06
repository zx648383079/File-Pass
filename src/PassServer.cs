﻿using NetFwTypeLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FilePass
{
    public class PassServer
    {
        public string Ip { get; private set; }

        public int Port { get; private set; }

        public int ThreadCount { get; set; } = 1;
        private readonly int splitSize = 1024 * 16;

        private readonly ConcurrentQueue<TcpClient> socketItems = new ConcurrentQueue<TcpClient>();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();

        public void Open(string ip, int port)
        {
            Ip = ip;
            Port = port;
            NetFwAddPorts("LargeFile", port, "TCP");
        }

        public void Close()
        {
            cancellationToken.Cancel();
            while (!socketItems.TryDequeue(out TcpClient tc))
            {
                tc.Close();
            }
        }

        public void Listen(string folder, Action<string, string, string> init, Action<long, long, string> progress)
        {
            var listenTask = new Task(() =>
            {
                var socket = new TcpListener(IPAddress.Parse(Ip), Port);
                socket.Start();
                while (true)
                {
                    var tc = socket.AcceptTcpClient();
                    socketItems.Enqueue(tc);
                }
            }, cancellationToken.Token);
            listenTask.Start();
            var receiveTask = new Task(() =>
            {
                while (true)
                {
                    if (socketItems.Count > 0)
                    {
                        for (int i = 0; i < ThreadCount; i++)
                        {
                            TcpClient tc;
                            while (!socketItems.TryDequeue(out tc))
                            {
                                Thread.Sleep(1);
                            }
                            Task.Factory.StartNew(() =>
                            {
                                ReceiveMessage(tc, folder, init, progress);
                            });
                            if (socketItems.Count < 1)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }, cancellationToken.Token);
            receiveTask.Start();
        }

        private void ReceiveMessage(TcpClient tc, string folder, Action<string, string, string> init, Action<long, long, string> progress)
        {
            NetworkStream ns;
            string filePath = string.Empty;
            try
            {
                ns = tc.GetStream();
                byte[] bytes = new byte[8];
                ns.Read(bytes, 0, 8);
                var fileLength = BitConverter.ToInt64(bytes, 0);
                bytes = new byte[8];
                ns.Read(bytes, 0, 8);
                var nameLength = BitConverter.ToInt32(bytes, 0);
                bytes = new byte[nameLength];
                ns.Read(bytes, 0, nameLength);
                var relativeFile = Encoding.UTF8.GetString(bytes).TrimEnd(char.MinValue);

                filePath = CreateFolder(folder, relativeFile, out string fileName);
                init?.Invoke(fileName, relativeFile, filePath);
                progress?.Invoke(0, fileLength, filePath);
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    long start = 0;
                    var size = splitSize;
                    while (start < fileLength)
                    {
                        var next = start + size;
                        if (next > fileLength)
                        {
                            size = Convert.ToInt32(fileLength - start);
                            next = fileLength;
                        }
                        byte[] content = new byte[size];
                        ns.CopyTo(fs, size);
                        progress?.Invoke(next, fileLength, filePath);
                        start = next;
                    }
                    ns.Close();
                }
            }
            catch (Exception)
            {
                progress?.Invoke(0, 0, filePath);
            }
            finally
            {
                try
                {
                    tc.Close();
                }
                catch (Exception)
                {
                    progress?.Invoke(0, 0, filePath);
                }
            }
        }

        private string CreateFolder(string folder, string path, out string fileName)
        {
            var items = path.Split('\\');
            if (items.Length < 2)
            {
                fileName = path;
                return folder + '\\' + path;
            }
            var file = new StringBuilder();
            file.Append(folder);
            for (int i = 0; i < items.Length - 1; i++)
            {
                var item = items[i];
                if (item.Length < 1 || string.IsNullOrEmpty(item))
                {
                    continue;
                }
                file.Append('\\');
                file.Append(item);
            }
            var dir = file.ToString();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            file.Append('\\');
            file.Append(items[items.Length - 1]);
            fileName = items[items.Length - 1];
            return file.ToString();
        }


        /// <summary>
        /// 添加防火墙例外端口
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="port">端口</param>
        /// <param name="protocol">协议(TCP、UDP)</param>
        public static void NetFwAddPorts(string name, int port, string protocol)
        {
            INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

            INetFwOpenPort objPort = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwOpenPort"));

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

            bool exist = false;
            foreach (INetFwOpenPort mPort in netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts)
            {
                if (objPort == mPort)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist) netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts.Add(objPort);
        }

    }
}
