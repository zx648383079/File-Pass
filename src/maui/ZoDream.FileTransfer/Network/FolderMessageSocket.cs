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
            Link.StopLoopReceive();
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
                if (!Link.AreYouReady())
                {
                    OnCompleted?.Invoke(MessageId, Folder, false);
                    return;
                }
                var fileItems = Disk.GetAllFile(Folder);
                foreach (var item in fileItems)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    item.Md5 = Disk.GetMD5(item.FileName);
                    SendFile(item, token);
                }
                Link.Send(SocketMessageType.PreClose);
                OnCompleted?.Invoke(MessageId, Folder, true);
            }, token);
        }

        protected void SendFile(FileInfoItem file, CancellationToken token)
        {
            Link.SendFile(file.RelativeFile, file.Md5, file.FileName, file.Length,
                (name, _, p, t) => {
                    OnProgress?.Invoke(MessageId, name, p, t);
                }, (name, _, isSuccess) => {
                    OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                }, token);
        }

        public Task ReceiveAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                while (Link.Connected)
                {
                    Link.ReceiveFile(Folder, true, (name, _, p, t) => {
                        OnProgress?.Invoke(MessageId, name, p, t);
                    }, (name, _, isSuccess) => {
                        OnCompleted?.Invoke(MessageId, name, isSuccess != false);
                    }, token);
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
