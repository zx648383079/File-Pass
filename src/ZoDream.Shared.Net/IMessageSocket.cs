using System;
using System.Threading.Tasks;

namespace ZoDream.Shared.Net
{
    /// <summary>
    /// 专门服务一个消息的连接
    /// </summary>
    public interface IMessageSocket: IDisposable
    {

        public string MessageId { get; }

        public event MessageProgressEventHandler? OnProgress;
        public event MessageCompletedEventHandler? OnCompleted;

        public Task SendAsync();

        public Task ReceiveAsync();

        public Task StopAsync();
    }
}
