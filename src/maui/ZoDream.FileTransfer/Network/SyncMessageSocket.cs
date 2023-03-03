using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
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

        private readonly SocketClient Link;
        private readonly string Folder;
        private Dictionary<string, FileInfoItem> FileItems = new();
        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;
        public string MessageId { get; private set; }
        private readonly CancellationTokenSource TokenSource = new();
        private FileSystemWatcher? Watcher;

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
                    SendCheckFile(item, token);
                }
                Link.Send(SocketMessageType.PreClose);
            }, token);
        }

        private void SendCheckFile(FileInfoItem file, CancellationToken token)
        {
            Link.Send(SocketMessageType.FileCheck);
            Link.SendText(file.RelativeFile);
            Link.SendText(file.Md5);
            Link.SendText(file.ModifyTime.ToString());
            FileItems.Add(file.RelativeFile, file);
            App.Repository.Logger.Debug($"Check File:{file.File}");
        }

        protected void SendFile(FileInfoItem file, CancellationToken token)
        {
            SendFile(file.RelativeFile, file.Md5, file.File, (p, t) => {
                OnProgress?.Invoke(MessageId, file.Name, p, t);
            }, token);
            App.Repository.Logger.Debug($"Send File:{file.File}");
        }

        public bool SendFile(string name,
            string md5,
            string fileName,
            Action<long, long>? onProgress = null,
            CancellationToken token = default)
        {
            var chunkSize = 200000;
            var modifyTime = File.GetLastWriteTime(fileName).ToString();
            using var reader = File.OpenRead(fileName);
            var length = reader.Length;
            if (length <= chunkSize)
            {
                Link.Send(SocketMessageType.File);
                Link.SendText(name);
                Link.SendText(md5);
                Link.SendText(modifyTime);
                Link.Send(length);
                Link.SendStream(reader, length);
                onProgress?.Invoke(length, length);
                return true;
            }
            var rate = length;
            var partItems = new List<string>();
            var i = 0;
            while (rate > 0)
            {
                if (!Link.Connected || token.IsCancellationRequested)
                {
                    return false;
                }
                var partName = $"{md5}_{i}";
                Link.Send(SocketMessageType.FilePart);
                Link.SendText(partName);
                var partLength = Math.Min(rate, chunkSize);
                Link.Send(partLength);
                Link.SendStream(reader, partLength);
                partItems.Add(partName);
                rate -= chunkSize;
                i++;
                onProgress?.Invoke(Math.Min(length - rate, length), length);
            }
            Link.Send(SocketMessageType.FileMerge);
            Link.SendText(name);
            Link.SendText(md5);
            Link.SendText(modifyTime);
            Link.SendText(string.Join(',', partItems));
            return true;
        }

        protected void SendFile(string fileName, CancellationToken token)
        {
            if (FileItems.TryGetValue(fileName, out var file))
            {
                SendFile(file, token);
            }
        }

        private void BindReceive()
        {
            var token = TokenSource.Token;
            var storage = App.Repository.Storage;
            Task.Factory.StartNew(() => {
                while (true)
                {
                    if (!Link.Connected || token.IsCancellationRequested)
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
                            File.Move(oldPath, newPath, true);
                        }
                        App.Repository.Logger.Debug($"Move File:{oldPath}->{newPath}");
                        continue;
                    }
                    else if (type == SocketMessageType.FileCheck)
                    {
                        var fileName = Link.ReceiveText();
                        var md5 = Link.ReceiveText();
                        var mTime = Link.ReceiveText();
                        var res = CheckFile(fileName, md5, mTime);
                        Link.Send(SocketMessageType.FileCheckResponse);
                        Link.SendText(fileName);
                        Link.Send(res);
                        App.Repository.Logger.Debug($"Check File:{fileName}->{res}");
                        continue;
                    }
                    else if (type == SocketMessageType.FileCheckResponse)
                    {
                        var fileName = Link.ReceiveText();
                        var shouldSend = Link.ReceiveBool();
                        if (shouldSend)
                        {
                            SendFile(fileName, token);
                        }
                        FileItems.Remove(fileName);
                        continue;
                    }
                    else if (type == SocketMessageType.FileDelete)
                    {
                        var delPath = Path.Combine(Folder, Link.ReceiveText());
                        App.Repository.Logger.Debug($"Delete File:{delPath}");
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
                        var modifyTime = Link.ReceiveText();
                        var length = Link.ReceiveContentLength();
                        var location = Path.Combine(Folder, fileName);
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
                        File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
                        continue;
                    }
                    else if (type == SocketMessageType.FileMerge)
                    {
                        var fileName = Link.ReceiveText();
                        var location = Path.Combine(Folder, fileName);
                        App.Repository.Logger.Debug($"Receive File:{location}");
                        var md5 = Link.ReceiveText();
                        var modifyTime = Link.ReceiveText();
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
                        File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
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
                        App.Repository.Logger.Warning($"File Receive Unknown Type:{type}");
                        App.Repository.NetHub.Close(Link);
                        OnCompleted?.Invoke(MessageId, Folder, false);
                        return;
                    }
                }
            }, token);
        }

        private bool CheckFile(string fileName, string md5, string mTime)
        {
            var path = Path.Combine(Folder, fileName);
            if (!File.Exists(path))
            {
                return true;
            }
            var time = File.GetLastWriteTime(path);
            if (time > DateTime.Parse(mTime))
            {
                return false;
            }
            return Disk.GetMD5(path) != md5;
        }

        private void BindWatcher()
        {
            if (Watcher != null)
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
