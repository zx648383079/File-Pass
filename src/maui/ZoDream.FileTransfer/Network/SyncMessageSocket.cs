using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class SyncMessageSocket : IMessageSocket
    {
        public SyncMessageSocket(
            SocketClient link, string messageId,
            string folder)
        {
            Link = link;
            Folder = folder;
            MessageId = messageId;
        }

        private SocketClient Link;
        private string Folder;
        public event MessageProgressEventHandler OnProgress;
        public event MessageCompletedEventHandler OnCompleted;
        public string MessageId { get; private set; }
        private readonly CancellationTokenSource TokenSource = new();
        private FileSystemWatcher Watcher;

        public void Dispose()
        {
            StopAsync();
        }


        public async Task SendAsync()
        {
            await SendFolderAsync();
            BindWatcher();
            BindReceive();
        }

        public Task ReceiveAsync()
        {
            BindWatcher();
            BindReceive();
            return Task.CompletedTask;
        }

        private Task SendFolderAsync()
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
            }, token);
        }

        protected void SendFile(FileInfoItem file, CancellationToken token)
        {
            Link.SendFile(file.RelativeFile, file.Md5, file.File, (p, t) => {
                OnProgress?.Invoke(MessageId, file.Name, p, t);
            }, token);
        }

        private void BindReceive()
        {
            var token = TokenSource.Token;
            var storage = App.Repository.Storage;
            Task.Factory.StartNew(() => {
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
                    } else if (type == SocketMessageType.FileRename)
                    {
                        var newPath = Path.Combine(Folder, Link.ReceiveText());
                        var oldPath = Path.Combine(Folder, Link.ReceiveText());
                        if (File.Exists(oldPath))
                        {
                            File.Move(oldPath, newPath);
                        }
                        continue;
                    } else if (type == SocketMessageType.FileDelete)
                    {
                        var delPath = Path.Combine(Folder, Link.ReceiveText());
                        if (File.Exists(delPath))
                        {
                            File.Delete(delPath);
                        }
                        continue;
                    }
                    else if (type == SocketMessageType.File)
                    {
                        var fileName = Link.ReceiveText();
                        var md5 = Link.ReceiveText();
                        var length = Link.ReceiveContentLength();
                        var location = Path.Combine(Folder, fileName);
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
                        var fileName = Link.ReceiveText();
                        var location = Path.Combine(Folder, fileName);
                        var md5 = Link.ReceiveText();
                        var partItems = Link.ReceiveText().Split(',');
                        var length = storage.CacheMergeFile(md5, partItems);
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
                        OnProgress?.Invoke(MessageId, "part", partLength, partLength);
                    }
                    else
                    {
                        App.Repository.NetHub.Close(Link);
                        OnCompleted?.Invoke(MessageId, Folder, false);
                        return;
                    }
                }
            }, token);
        }

        private void BindWatcher()
        {
            if (Watcher == null)
            {
                Watcher.Dispose();
            }
            Watcher = new FileSystemWatcher(Folder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true
            };
            Watcher.Created += Watcher_Created;
            Watcher.Renamed += Watcher_Renamed;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Changed += Watcher_Changed;
        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _ = SendFileAsync(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Link.Send(SocketMessageType.FileDelete);
            Link.SendText(Path.GetRelativePath(Folder, e.FullPath));
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Link.Send(SocketMessageType.FileRename);
            Link.SendText(Path.GetRelativePath(Folder, e.FullPath));
            Link.SendText(Path.GetRelativePath(Folder, e.OldFullPath));
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            
        }

        private Task SendFileAsync(string fullName)
        {
            var token = TokenSource.Token;
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }
            return Task.Factory.StartNew(() => {
                var fileName = Path.GetRelativePath(Folder, fullName);
                var md5 = Disk.GetMD5(fullName);
                Link.SendFile(fileName, md5, fullName, (p, t) => {
                    OnProgress?.Invoke(MessageId, fileName, p, t);
                });
            });
        }

        public Task StopAsync()
        {
            TokenSource.Cancel();
            Watcher?.Dispose();
            OnCompleted?.Invoke(MessageId, Folder, false);
            return Task.CompletedTask;
        }
    }
}
