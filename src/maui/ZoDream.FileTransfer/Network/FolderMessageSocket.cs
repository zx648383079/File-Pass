using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class FolderMessageSocket : IMessageSocket
    {
        public FolderMessageSocket(
            SocketClient link, string messageId,
            string folder)
        {
            Link = link;
            Folder = folder;
            MessageId = messageId;
        }

        private readonly SocketClient Link;
        private readonly string Folder;
        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;
        public string MessageId { get; private set; }
        private readonly CancellationTokenSource TokenSource = new();

        public void Dispose()
        {
            StopAsync();
        }


        public Task SendAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                var fileItems = Disk.GetAllFile(Folder);
                foreach (var item in fileItems)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    item.Md5 = Disk.GetMD5(item.File);
                    SendFile(item, token);
                }
                Link.Send(SocketMessageType.PreClose);
                OnCompleted?.Invoke(MessageId, Folder, true);
            }, token);
        }

        protected void SendFile(FileInfoItem file, CancellationToken token)
        {
            Link.SendFile(file.RelativeFile, file.Md5, file.File, (p, t) => {
                OnProgress?.Invoke(MessageId, file.Name, p, t);
            }, token);
            App.Repository.Logger.Debug($"Send File:{file.File}");
        }

        public Task ReceiveAsync()
        {
            var token = TokenSource.Token;
            var storage = App.Repository.Storage;
            return Task.Factory.StartNew(() => {
                var fileName = string.Empty;
                var md5 = string.Empty;
                var length = 0L;
                var location = string.Empty;
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        App.Repository.NetHub.Close(Link);
                        OnCompleted?.Invoke(MessageId, Folder, false);
                        return;
                    }
                    var type = Link.ReceiveMessageType();
                    if (type == SocketMessageType.PreClose)
                    {
                        App.Repository.NetHub.Close(Link);
                        OnCompleted?.Invoke(MessageId, Folder, true);
                        return;
                    } else if (type == SocketMessageType.File)
                    {
                        fileName = Link.ReceiveText();
                        md5 = Link.ReceiveText();
                        length = Link.ReceiveContentLength();
                        location = Path.Combine(Folder, fileName);
                        App.Repository.Logger.Debug($"Receive File:{location}");
                        using (var fs = storage.CacheWriter(md5))
                        {
                            Link.ReceiveStream(fs, length);
                        }
                        OnProgress?.Invoke(MessageId, fileName, length, length);
                        if (md5 != storage.CacheFileMD5(md5))
                        {
                            storage.CacheRemove(md5);
                            continue;
                        }
                        storage.CacheMove(md5, location);
                        continue;
                    }
                    else if (type == SocketMessageType.FileMerge)
                    {
                        fileName = Link.ReceiveText();
                        location = Path.Combine(Folder, fileName);
                        App.Repository.Logger.Debug($"Receive File:{location}");
                        md5 = Link.ReceiveText();
                        var partItems = Link.ReceiveText().Split(',');
                        length = storage.CacheMergeFile(md5, partItems);
                        if (length <= 0 || md5 != storage.CacheFileMD5(md5))
                        {
                            storage.CacheRemove(partItems);
                            storage.CacheRemove(md5);
                            continue;
                        }
                        storage.CacheRemove(partItems);
                        storage.CacheMove(md5, location);
                        OnProgress?.Invoke(MessageId, fileName, length, length);
                        continue;
                    }
                    else if (type == SocketMessageType.FilePart)
                    {
                        var partName = Link.ReceiveText();
                        var partLength = Link.ReceiveContentLength();
                        using (var fs = storage.CacheWriter(partName))
                        {
                            Link.ReceiveStream(fs, partLength);
                        }
                        length += partLength;
                        OnProgress?.Invoke(MessageId, "part", length, 0L);
                    }
                    else
                    {
                        App.Repository.Logger.Warning($"File Receive Unknown Type:{type}");
                        App.Repository.NetHub.Close(Link);
                        OnCompleted?.Invoke(MessageId, Folder, false);
                        return;
                    }
                }
            }, token);
        }

        public Task StopAsync()
        {
            TokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
