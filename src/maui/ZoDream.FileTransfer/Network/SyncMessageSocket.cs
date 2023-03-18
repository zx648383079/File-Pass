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
            Hub = App.Repository.NetHub;
            Link = link;
            Folder = folder;
            MessageId = messageId;
            Link.StopLoopReceive();
        }

        private readonly SocketHub Hub;
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
            if (!Link.AreYouReady())
            {
                OnCompleted?.Invoke(MessageId, Folder, false);
                return;
            }
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
                    item.Md5 = Disk.GetMD5(item.FileName);
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
            App.Repository.Logger.Debug($"Check File:{file.FileName}");
        }

        protected void SendFile(FileInfoItem file, CancellationToken token)
        {
            SendFile(file.RelativeFile, file.Md5, file.FileName, file.Length,
               (name, _, p, t) => {
                   OnProgress?.Invoke(MessageId, name, p, t);
               }, (name, _, isSuccess) => {
                   OnCompleted?.Invoke(MessageId, name, isSuccess != false);
               }, token);
            App.Repository.Logger.Debug($"Send File:{file.FileName}");
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
            Task.Factory.StartNew(() => {
                while (Link.Connected)
                {
                    ReceiveFile(Folder, (name, _, p, t) => {
                        OnProgress?.Invoke(MessageId, name, p, t);
                    }, (name, _, isSuccess) => {
                        OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                    }, token);
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
                var length = 0L;
                var md5 = string.Empty;
                using (var fs = File.OpenRead(fullName))
                {
                    length = fs.Length;
                    md5 = Disk.GetMD5(fs);
                }
                SendFile(fileName, md5, fullName, length, 
                    (name, _, p, t) => {
                    OnProgress?.Invoke(MessageId, name, p, t);
                }, (name, _, isSuccess) => {
                    OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                });
            });
        }

        /// <summary>
        /// 只接收一个文件
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="onProgress"></param>
        /// <param name="onCompleted"></param>
        /// <param name="token"></param>
        public void ReceiveFile(string folder,
            FileProgressEventHandler? onProgress = null,
            FileCompletedEventHandler? onCompleted = null,
            CancellationToken token = default)
        {
            var fileName = string.Empty;
            var location = string.Empty;
            var storage = App.Repository.Storage;
            while (true)
            {
                if (!Link.Connected || token.IsCancellationRequested)
                {
                    Hub?.Close(Link);
                    OnCompleted?.Invoke(MessageId, Folder, false);
                    // onCompleted?.Invoke(fileName, location, false);
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
                    continue;
                }
                if (type == SocketMessageType.PreClose)
                {
                    Hub?.Logger.Debug("Receive Complete");
                    Hub?.Close(Link);
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
                    continue;
                }
                else if (type == SocketMessageType.FileCheckResponse)
                {
                    fileName = Link.ReceiveText();
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
                else if (type == SocketMessageType.FileCheck)
                {
                    fileName = Link.ReceiveText();
                    var md5 = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    location = Path.Combine(folder, fileName);
                    var shouldSend = storage.CheckFile(location, md5);
                    Link.Send(SocketMessageType.FileCheckResponse);
                    Link.SendText(fileName);
                    Link.Send(shouldSend);
                    Hub?.Logger.Debug($"Receive Check: {fileName}->{shouldSend}");
                    if (!shouldSend)
                    {
                        onCompleted?.Invoke(fileName, location, null);
                        return;
                    }
                    continue;
                }
                else if (type == SocketMessageType.File)
                {
                    fileName = Link.ReceiveText();
                    location = Path.Combine(folder, fileName);
                    var md5 = Link.ReceiveText();
                    var modifyTime = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    using (var fs = storage.CacheWriter(md5))
                    {
                        Link.ReceiveStream(fs, length);
                    }
                    onProgress?.Invoke(fileName, location, length, length);
                    if (md5 != storage.CacheFileMD5(md5))
                    {
                        Link.Send(SocketMessageType.ReceivedError);
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        onCompleted?.Invoke(fileName, location, false);
                        storage.CacheRemove(md5);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    storage.CacheMove(md5, location);
                    File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
                    Link.Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    onCompleted?.Invoke(fileName, location, true);
                    return;
                }
                else if (type == SocketMessageType.FileMerge)
                {
                    fileName = Link.ReceiveText();
                    location = Path.Combine(folder, fileName);
                    var md5 = Link.ReceiveText();
                    var modifyTime = Link.ReceiveText();
                    var length = Link.ReceiveContentLength();
                    Link.Jump();
                    // var partItems = ReceiveText().Split(',');
                    if (md5 != storage.CacheFileMD5(md5))
                    {
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        storage.CacheRemove(md5);
                        Link.Send(SocketMessageType.ReceivedError);
                        onCompleted?.Invoke(fileName, location, false);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    storage.CacheMove(md5, location);
                    File.SetLastWriteTime(location, DateTime.Parse(modifyTime));
                    Link.Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    onCompleted?.Invoke(fileName, location, true);
                    return;
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = Link.ReceiveText();
                    fileName = Link.ReceiveText();
                    var md5 = partName.Split('_')[0];
                    location = Path.Combine(folder, fileName);
                    var rang = Link.ReceiveText().Split(new char[] { '-', '/' });
                    var length = Convert.ToInt64(rang[2]);
                    var startPos = Convert.ToInt64(rang[0]);
                    var endPos = Convert.ToInt64(rang[1]);
                    var partLength = Link.ReceiveContentLength();
                    using (var fs = storage.CacheWriter(md5, true))
                    {
                        fs.SetLength(length);
                        fs.Seek(startPos, SeekOrigin.Begin);
                        Link.ReceiveStream(fs, partLength);
                    }
                    Hub?.Logger.Debug($"Receive File Part: {fileName}[{startPos}-{endPos}]");
                    onProgress?.Invoke(fileName, location, endPos, length);
                    Link.Send(SocketMessageType.Received);
                    continue;
                }
                else
                {
                    onCompleted?.Invoke(fileName, location, false);
                    Hub?.Logger.Error("Lose pack");
                    return;
                }
            }
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
            FileProgressEventHandler? onProgress = null,
            FileCompletedEventHandler? onCompleted = null,
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
                    onCompleted?.Invoke(name, fileName, null);
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
                onProgress?.Invoke(name, fileName, length, length);
                Hub?.Logger.Debug($"File Send :{name}");
                type = Link.ReceiveMessageType();
                onCompleted?.Invoke(name, fileName, type == SocketMessageType.Received);
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
                    onCompleted?.Invoke(name, fileName, false);
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
                onProgress?.Invoke(name, fileName, endPos, length);
                Hub?.Logger.Debug($"File Send Part :{name}[{startPos}-{endPos}]");
                type = Link.ReceiveMessageType();
                if (type != SocketMessageType.Received)
                {
                    onCompleted?.Invoke(name, fileName, false);
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
            onProgress?.Invoke(name, fileName, length, length);
            Hub?.Logger.Debug($"File Send Merge :{name}");
            type = Link.ReceiveMessageType();
            onCompleted?.Invoke(name, fileName, type == SocketMessageType.Received);
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
}
