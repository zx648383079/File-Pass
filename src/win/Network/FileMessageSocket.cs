using NetFwTypeLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class FileMessageSocket : IDisposable
    {

        public FileMessageSocket(
            SocketHub hub,
            string ip,
            int port
            )
        {
            Hub = hub;
            Ip = ip;
            Port = port;
        }

        private readonly SocketHub Hub;

        public string Ip { get; private set; }

        public int Port { get; set; }

        private readonly ConcurrentQueue<FileInfoItem> FileItems = new();

        public void Add( FileInfoItem item )
        {
            FileItems.Enqueue(item);
        }

        public void Add(IEnumerable<FileInfoItem> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public int UseThread(int maxCount, CancellationToken token)
        {
            var i = 0;
            for (; i < maxCount; i++)
            {
                if (FileItems.IsEmpty)
                {
                    return i;
                }
                
                if (FileItems.TryDequeue(out var file))
                {
                    var client = Hub.Add(Ip, Port);
                    if (client is null)
                    {
                        Add(file);
                        return i;
                    }
                    Task.Factory.StartNew(() => {
                        var md5 = Disk.GetMD5(file.File);
                        client.SendFile(file.RelativeFile, md5, file.File, token);
                        client.Send(SocketMessageType.PreClose);
                        Hub.Logger.Debug($"Send Complete: {file.RelativeFile}");
                    }, token);
                }
            }
            return i;
        }

        public void Dispose()
        {
        }

        
    }
}
