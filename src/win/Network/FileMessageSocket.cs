using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public void Add(FileInfoItem item )
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
                    var client = string.IsNullOrEmpty(Ip) ? Hub.Get() : Hub.Add(Ip, Port);
                    if (client is null)
                    {
                        Add(file);
                        return i;
                    }
                    client.IsBusy = true;
                    client.StopLoopReceive();
                    Task.Factory.StartNew(() => {
                        if (string.IsNullOrEmpty(file.Md5))
                        {
                            file.Md5 = Disk.GetMD5(file.FileName);
                        }
                        client.SendFile(file.RelativeFile, 
                            file.Md5, 
                            file.FileName, file.Length, token);
                        if (!client.IsPassively)
                        {
                            client.Send(SocketMessageType.PreClose);
                            Hub.Close(client);
                        }
                        client.IsBusy = false;
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
