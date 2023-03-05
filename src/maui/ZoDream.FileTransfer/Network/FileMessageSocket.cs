﻿using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class FileMessageSocket : IMessageSocket
    {
        public FileMessageSocket(
            SocketClient link, string messageId, 
            string name, string fileName)
        {
            Link = link;
            Name = name;
            FileName = fileName;
            MessageId = messageId;
            Link.StopLoopReceive();
        }

        private readonly SocketClient Link;
        private string Name;
        private readonly string FileName;
        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;
        public string MessageId { get; private set; }
        private CancellationTokenSource TokenSource = new();

        public void Dispose()
        {
            StopAsync();
        }

        public Task SendAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                var length = 0L;
                var md5 = string.Empty;
                using (var fs = File.OpenRead(FileName))
                {
                    length = fs.Length;
                    md5 = Disk.GetMD5(fs);
                }
                Link.SendFile(Name, md5, FileName, length, 
                    (name, _, p, t) => {
                    OnProgress?.Invoke(MessageId, name, p, t);
                }, (name, _, isSuccess) => {
                    OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                }, token);
            }, token);
        }

        public Task ReceiveAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                Link.ReceiveFile(FileName, true, (name, _, p, t) => {
                    OnProgress?.Invoke(MessageId, name, p, t);
                }, (name, _, isSuccess) => {
                    OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                }, token);
                // 线程由接收方结束
                App.Repository.NetHub.Close(Link);
            }, token);
        }

        public Task StopAsync()
        {
            TokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
