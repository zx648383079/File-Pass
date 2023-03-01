using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class FileMessageSocket : IMessageSocket
    {
        public FileMessageSocket(
            SocketClient link, string messageId, 
            string name, string fileName, 
            Action<long,long> func)
        {
            Link = link;
            Name = name;
            FileName = fileName;
            OnProgress = func;
            MessageId = messageId;
        }

        private SocketClient Link;
        private string Name;
        private string FileName;
        private Action<long, long> OnProgress;
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
                var md5 = Disk.GetMD5(FileName);
                Link.SendFile(Name, md5, FileName, OnProgress, token);
            }, token);
        }

        public Task ReceiveAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                // TODO
            }, token);
        }

        public Task StopAsync()
        {
            TokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
