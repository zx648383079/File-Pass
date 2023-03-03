using ZoDream.FileTransfer.Utils;

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
                var md5 = Disk.GetMD5(FileName);
                var res = Link.SendFile(Name, md5, FileName, (p, t) => {
                    OnProgress?.Invoke(MessageId, Name, p, t);
                }, token);
                App.Repository.Logger.Debug($"Send File:{FileName}");
                OnCompleted?.Invoke(MessageId, Name, res);
            }, token);
        }

        public Task ReceiveAsync()
        {
            var token = TokenSource.Token;
            return Task.Factory.StartNew(() => {
                Name = Link.ReceiveFile(FileName, (p, t) => {
                    OnProgress?.Invoke(MessageId, Name, p, t);
                }, token);
                OnCompleted?.Invoke(MessageId, Name, !string.IsNullOrEmpty(Name));
                App.Repository.Logger.Debug($"Receive File:{Name}");
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
