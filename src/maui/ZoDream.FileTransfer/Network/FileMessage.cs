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
        public string Md5 { get; set; }

        public MessageItem ConverterTo()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Md5 = socket.ReceiveText();
            var length = socket.ReceiveContentLength();
            using var reader = App.Repository.Repository.CacheWriter(Md5);
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
        public string FileName { get; set; }

        public long Length { get; set; }

        public MessageItem ConverterTo()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            FileName = socket.ReceiveText();
            Length = socket.ReceiveContentLength();
            using var reader = App.Repository.Repository.CacheWriter(FileName);
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
        const string Separator = ",";

        public string Md5 { get; set; }

        public IList<string> PartItems { get; set; }

        public MessageItem ConverterTo()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReceiveAsync(SocketClient socket)
        {
            Md5 = socket.ReceiveText();
            var partName = socket.ReceiveText();
            PartItems = partName.Split(Separator);
            return Task.FromResult(true);
        }

        public Task<bool> SendAsync(SocketClient socket)
        {
            socket.SendText(SocketMessageType.FileMerge, Md5);
            socket.SendText(string.Join(Separator, PartItems));
            return Task.FromResult(true);
        }
    }
}
