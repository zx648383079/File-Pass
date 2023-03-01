using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class FolderMessageSocket : IMessageSocket
    {
        public string MessageId => throw new NotImplementedException();

        public void Dispose()
        {
            StopAsync();
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
        }
    }
}
