using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Repositories;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class SyncMessageSocket : IMessageSocket
    {
        public SyncMessageSocket(
            SocketClient link, string messageId,
            string folder)
        {
            Hub = App.Repository.NetHub;
            Link = link;
            Folder = folder;
            MessageId = messageId;
            Link.StopLoopReceive();
            Storage = App.Repository.Storage;
        }

        private readonly SocketHub Hub;
        private readonly SocketClient Link;
        private readonly string Folder;
        private readonly ConcurrentQueue<FileSyncItem> FileItems = new();
        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;
        public string MessageId { get; private set; }
        private readonly CancellationTokenSource TokenSource = new();
        private FileSystemWatcher? Watcher;
        private readonly StorageRepository Storage;

        public void Add(string fileName)
        {
            foreach (var item in FileItems)
            {
                if (item.FileName == fileName)
                {
                    return;
                }
            }
            var info = new FileInfo(fileName);
            FileItems.Enqueue(new FileSyncItem()
            {
                Name = info.Name,
                FileName = fileName,
                RelativeFileName = Path.GetRelativePath(Folder, fileName)
            });
        }

        public void Add(FileAction action, string fileName)
        {
            if (action == FileAction.Delete)
            {
                foreach (var item in FileItems)
                {
                    if (item.FileName == fileName)
                    {
                        item.IsExpired = true;
                    }
                }
            }
            FileItems.Enqueue(new FileSyncItem()
            {
                Action = action,
                Name = Path.GetFileName(fileName),
                FileName = fileName,
                RelativeFileName = Path.GetRelativePath(Folder, fileName)
            });
        }

        public void Add(FileAction action, string fileName, string oldFileName)
        {
            if (action == FileAction.Rename)
            {
                foreach (var item in FileItems)
                {
                    if (item.FileName == fileName || item.FileName == oldFileName)
                    {
                        item.IsExpired = true;
                    }
                }
            }
            FileItems.Enqueue(new FileSyncItem()
            {
                Action = action,
                Name = Path.GetFileName(fileName),
                FileName = fileName,
                RelativeFileName = Path.GetRelativePath(Folder, fileName),
                OldFileName = Path.GetRelativePath(Folder, oldFileName),
            });
        }

        public FileSyncItem? Get()
        {
            while (FileItems.TryDequeue(out var item))
            {
                if (!item.IsExpired)
                {
                    return item;
                }
            }
            return null;
        }

        public void Dispose()
        {
            FileItems.Clear();
            StopAsync();
        }


        public async Task SendAsync()
        {
            if (!Link.AreYouReady())
            {
                OnCompleted?.Invoke(MessageId, Folder, false);
                return;
            }
            await SendFolderAsync();
            BindWatcher();
            ReceiveAndSend(true);
        }

        public Task ReceiveAsync()
        {
            BindWatcher();
            ReceiveAndSend(false);
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
                    FileItems.Enqueue(new FileSyncItem()
                    {
                        FileName = item.FileName,
                        RelativeFileName = item.RelativeFile,
                        Name = item.Name,
                    });
                }
            }, token);
        }

        private void ReceiveAndSend(bool isSend)
        {
            var token = TokenSource.Token;
            Task.Factory.StartNew(() => {
                if (isSend)
                {
                    if (!Link.AreYouReady())
                    {
                        OnCompleted?.Invoke(MessageId, Folder, false);
                        return;
                    }
                }
                while (Link.Connected && !token.IsCancellationRequested)
                {
                    if (isSend)
                    {
                        SenderDo(token);
                    } else
                    {
                        ReceiverDo(token);
                    }
                }
            }, token);
        }

        private void SenderDo(CancellationToken token)
        {
            var item = Get();
            if (item == null)
            {
                Link.Send(SocketMessageType.Null);
                Receive(token);
            } else
            {
                Send(item, token);
            }
        }

        private void ReceiverDo(CancellationToken token)
        {
            Receive(token);
        }
        private void Receive(CancellationToken token)
        {
            while (true)
            {
                if (!Link.Connected || token.IsCancellationRequested)
                {
                    return;
                }
                var type = Link.ReceiveMessageType();
                if (type == SocketMessageType.Ready)
                {
                    // 询问是否准备好了
                    var isRequest = Link.ReceiveBool();
                    if (isRequest)
                    {
                        Link.SendReady(false);
                    }
                    return;
                } else if (type == SocketMessageType.PreClose)
                {
                    return;
                } else if (type == SocketMessageType.Null)
                {
                    // 表明对方没有文件发送，你可以发送文件
                    Send(token);
                    return;
                }
                else if (type == SocketMessageType.FileRename)
                {
                    var newPath = Path.Combine(Folder, Link.ReceiveText());
                    var oldPath = Path.Combine(Folder, Link.ReceiveText());
                    if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath, true);
                    }
                    Hub?.Logger.Debug($"Move File:{oldPath}->{newPath}");
                    return;
                }
                else if (type == SocketMessageType.FileDelete)
                {
                    var delPath = Path.Combine(Folder, Link.ReceiveText());
                    App.Repository.Logger.Debug($"Delete File:{delPath}");
                    if (File.Exists(delPath))
                    {
                        File.Delete(delPath);
                    }
                    return;
                }
                else if (type == SocketMessageType.FileCheck)
                {
                    var fileName = Link.ReceiveText();
                    var md5 = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    var location = Path.Combine(Folder, fileName);
                    var shouldSend = Storage.CheckFile(location, md5);
                    Link.Send(SocketMessageType.FileCheckResponse);
                    Link.SendText(fileName);
                    Link.Send(shouldSend);
                    Hub?.Logger.Debug($"Receive Check: {fileName}->{shouldSend}");
                    if (!shouldSend)
                    {
                        return;
                    }
                    continue;
                }
                else if (type == SocketMessageType.File)
                {
                    var fileName = Link.ReceiveText();
                    var location = Path.Combine(Folder, fileName);
                    var md5 = Link.ReceiveText();
                    var modifyTime = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    using (var fs = Storage.CacheWriter(md5))
                    {
                        Link.ReceiveStream(fs, length);
                    }
                    OnProgress?.Invoke(fileName, location, length, length);
                    if (md5 != Storage.CacheFileMD5(md5))
                    {
                        Link.Send(SocketMessageType.ReceivedError);
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        Storage.CacheRemove(md5);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    Storage.CacheMove(md5, location);
                    File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
                    Link.Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    return;
                }
                else if (type == SocketMessageType.FileMerge)
                {
                    var fileName = Link.ReceiveText();
                    var location = Path.Combine(Folder, fileName);
                    var md5 = Link.ReceiveText();
                    var modifyTime = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    Link.Jump();
                    // var partItems = ReceiveText().Split(',');
                    if (md5 != Storage.CacheFileMD5(md5))
                    {
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        Storage.CacheRemove(md5);
                        Link.Send(SocketMessageType.ReceivedError);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    Storage.CacheMove(md5, location);
                    File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
                    Link.Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    return;
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = Link.ReceiveText();
                    var fileName = Link.ReceiveText();
                    var md5 = partName.Split('_')[0];
                    var location = Path.Combine(Folder, fileName);
                    var rang = Link.ReceiveText().Split(new char[] { '-', '/' });
                    var length = Convert.ToInt64(rang[2]);
                    var startPos = Convert.ToInt64(rang[0]);
                    var endPos = Convert.ToInt64(rang[1]);
                    var partLength = Link.ReceiveContentLength();
                    using (var fs = Storage.CacheWriter(md5, true))
                    {
                        fs.SetLength(length);
                        fs.Seek(startPos, SeekOrigin.Begin);
                        Link.ReceiveStream(fs, partLength);
                    }
                    Hub?.Logger.Debug($"Receive File Part: {fileName}[{startPos}-{endPos}]");
                    OnProgress?.Invoke(fileName, location, endPos, length);
                    Link.Send(SocketMessageType.Received);
                    continue;
                }
                else
                {
                    Hub?.Logger.Error("Lose pack");
                    return;
                }
            }
        }

        private void Send(CancellationToken token)
        {
            var item = Get();
            if (item == null)
            {
                Link.Send(SocketMessageType.Null);
            }
            else
            {
                Send(item, token);
            }
        }

        private void Send(FileSyncItem item, CancellationToken token)
        {
            switch (item.Action)
            {
                case FileAction.Delete:
                    Link.Send(SocketMessageType.FileDelete);
                    Link.SendText(item.RelativeFileName);
                    break;
                case FileAction.Rename:
                    Link.Send(SocketMessageType.FileRename);
                    Link.SendText(item.RelativeFileName);
                    Link.SendText(item.OldFileName);
                    break;
                default:
                    SendFile(item.RelativeFileName, item.FileName, token);
                    break;
            }
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
            Watcher?.Dispose();
#if WINDOWS || MACCATALYST
            Watcher = new FileSystemWatcher(Folder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true
            };
            Watcher.Created += Watcher_Created;
            Watcher.Renamed += Watcher_Renamed;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Changed += Watcher_Changed;
#endif
        }


        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Add(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Add(FileAction.Delete, e.FullPath);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Add(FileAction.Rename, e.FullPath, e.OldFullPath);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Add(e.FullPath);
        }

        private void SendFile(string name, string fullName, CancellationToken token)
        {
            var length = 0L;
            var md5 = string.Empty;
            try
            {
                using (var fs = File.OpenRead(fullName))
                {
                    length = fs.Length;
                    md5 = Disk.GetMD5(fs);
                }
            }
            catch (Exception ex)
            {
                Hub?.Logger.Error($"[{name}]File Send Error: {ex.Message}");
                return;
            }
            SendFile(name, md5, fullName, length);
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="md5"></param>
        /// <param name="fileName"></param>
        /// <param name="length"></param>
        /// <param name="onProgress"></param>
        /// <param name="onCompleted"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool SendFile(string name,
            string md5,
            string fileName,
            long length,
            CancellationToken token = default)
        {
            Link.Send(SocketMessageType.FileCheck);
            Link.SendText(name);
            Link.SendText(md5);
            Link.Send(length);
            Hub?.Logger.Debug($"Check File:{name}");
            var type = Link.ReceiveMessageType();
            if (type == SocketMessageType.FileCheckResponse)
            {
                var shouldSendName = Link.ReceiveText();
                var shouldSend = Link.ReceiveBool();
                if (!shouldSend)
                {
                    Hub?.Logger.Debug($"Quicky Send :{name}");
                    // 秒传
                    return true;
                }
            }
            var modifyTime = File.GetLastWriteTime(fileName).ToString();
            var chunkSize = Link.FileChunkSize;
            using var reader = File.OpenRead(fileName);
            if (length <= chunkSize)
            {
                Link.Send(SocketMessageType.File);
                Link.SendText(name);
                Link.SendText(md5);
                Link.SendText(modifyTime);
                Link.Send(length);
                Link.SendStream(reader, length);
                OnProgress?.Invoke(name, fileName, length, length);
                Hub?.Logger.Debug($"File Send :{name}");
                type = Link.ReceiveMessageType();
                return type == SocketMessageType.Received;
            }
            var partItems = new List<string>();
            var i = 0;
            var startPos = 0L;
            var endPos = 0L;
            while (endPos < length)
            {
                if (!Link.Connected || token.IsCancellationRequested)
                {
                    return false;
                }
                var partName = $"{md5}_{i}";
                Link.Send(SocketMessageType.FilePart);
                Link.SendText(partName);
                Link.SendText(name);
                var partLength = Math.Min(length - startPos, chunkSize);
                endPos = startPos + partLength;
                Link.SendText($"{startPos}-{endPos}/{length}");
                Link.Send(partLength);
                Link.SendStream(reader, partLength);
                partItems.Add(partName);
                i++;
                OnProgress?.Invoke(name, fileName, endPos, length);
                Hub?.Logger.Debug($"File Send Part :{name}[{startPos}-{endPos}]");
                type = Link.ReceiveMessageType();
                if (type != SocketMessageType.Received)
                {
                    Hub?.Logger.Debug("Not Receive Reply");
                    return false;
                }
                startPos = endPos;
            }
            Link.Send(SocketMessageType.FileMerge);
            Link.SendText(name);
            Link.SendText(md5);
            Link.SendText(modifyTime);
            Link.Send(length);
            Link.SendText(string.Join(",", partItems));
            OnProgress?.Invoke(name, fileName, length, length);
            Hub?.Logger.Debug($"File Send Merge :{name}");
            type = Link.ReceiveMessageType();
            return type == SocketMessageType.Received;
        }


        public Task StopAsync()
        {
            TokenSource.Cancel();
            Watcher?.Dispose();
            OnCompleted?.Invoke(MessageId, Folder, false);
            return Task.CompletedTask;
        }
    }

    public class FileSyncItem
    {

        public string Name { get; set; } = string.Empty;
        public string RelativeFileName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        public string OldFileName { get; set; } = string.Empty;

        public FileAction Action { get; set; } = FileAction.Send;

        public bool IsExpired { get; set; } = false;
    }

    public enum FileAction
    {
        Send,
        Delete,
        Rename,
    }
}
