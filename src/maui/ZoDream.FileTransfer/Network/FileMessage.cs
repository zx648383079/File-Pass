using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    
    public class FileMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; } = SocketMessageType.File;
        public string Md5 { get; set; }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Md5 = socket.ReceiveText();
            var length = socket.ReceiveContentLength();
            using var reader = App.Repository.Storage.CacheWriter(Md5);
            socket.ReceiveStream(reader, length);
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            // socket.SendText(SocketMessageType.FilePart, FileName);
            throw new NotImplementedException();
        }
    }

    public class FilePartMessage : ISocketMessage
    {
        public SocketMessageType Type { get; set; } = SocketMessageType.FilePart;
        public string FileName { get; set; }

        public long Length { get; set; }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            FileName = socket.ReceiveText();
            Length = socket.ReceiveContentLength();
            using var reader = App.Repository.Storage.CacheWriter(FileName);
            socket.ReceiveStream(reader, Length);
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            // socket.SendText(SocketMessageType.FilePart, FileName);
            throw new NotImplementedException();
        }
    }

    public class FileMergeMessage : ISocketMessage
    {
        public const string Separator = ",";

        public SocketMessageType Type { get; set; } = SocketMessageType.FileMerge;

        public string Md5 { get; set; }

        public string FileName { get; set; }

        public IList<string> PartItems { get; set; }


        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Md5 = socket.ReceiveText();
            var partName = socket.ReceiveText();
            FileName = socket.ReceiveText();
            PartItems = partName.Split(Separator);
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.SendText(SocketMessageType.FileMerge, Md5);
            socket.SendText(string.Join(Separator, PartItems));
            socket.SendText(FileName);
            return Task.FromResult(true);
        }
    }
}
