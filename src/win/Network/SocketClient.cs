using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public string Ip { get; private set; } = string.Empty;

        public int Port { get; private set; } = 80;

        private readonly int ChunkSize = 500;
        private readonly int FileChunkSize = 100 * 1024;
        private readonly Socket ClientSocket;
        private readonly CancellationTokenSource CancellationToken = new();
        public SocketHub? Hub { get; set; }
        public SocketClient(Socket socket)
        {
            ClientSocket = socket;
        }
        public SocketClient(Socket socket, string ip, int port) : this(socket)
        {
            Ip = ip;
            Port = port;
        }

        public bool Connected => ClientSocket.Connected;


        #region 接受消息
        private SocketMessageType ReceiveMessageType()
        {
            var buffer = new byte[1];
            ClientSocket.Receive(buffer);
            var type = (SocketMessageType)buffer[0];
            Hub?.Logger.Debug($"Message Type: {buffer[0]}[{type}]");
            return type;
        }

        private byte[] Receive(long length)
        {
            if (length <= 0 || length > FileChunkSize)
            {
                return Array.Empty<byte>();
            }
            var buffer = new byte[length];
            try
            {
                ClientSocket.Receive(buffer);
            }
            catch (Exception ex)
            {
                Hub?.Logger.Error(ex.Message);
            }
            return buffer;
        }

        private long ReceiveContentLength()
        {
            return BitConverter.ToInt64(Receive(8), 0);
        }

        private string ReceiveText()
        {
            var length = ReceiveContentLength();
            if (length > FileChunkSize)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(Receive(length));
        }

        public void ReceiveStream(Stream writer, long length)
        {
            var rate = length;
            while (rate > 0)
            {
                var buffer = Receive(Math.Min(rate, ChunkSize));
                if (buffer.Length < 1)
                {
                    return;
                }
                writer.Write(buffer, 0, buffer.Length);
                rate -= buffer.Length;
            }
        }

        public string ReceiveText(long length)
        {
            var buffer = Receive(length);
            return Encoding.UTF8.GetString(buffer);
        }

        public bool ReceiveBool()
        {
            var buffer = Receive(1);
            return Convert.ToBoolean(buffer[0]);
        }
        /// <summary>
        /// 跳过
        /// </summary>
        public void Jump()
        {
            Jump(ReceiveContentLength());
        }

        /// <summary>
        /// 跳过指定字节
        /// </summary>
        /// <param name="length"></param>
        public void Jump(long length)
        {
            var rate = length;
            while (rate > 0)
            {
                var buffer = Receive(Math.Min(rate, ChunkSize));
                if (buffer.Length < 1)
                {
                    return;
                }
                rate -= buffer.Length;
            }
        }
        #endregion

        #region 发送消息

        private void Send(byte[] buffer)
        {
            if (!ClientSocket.Connected)
            {
                return;
            }
            ClientSocket.Send(buffer);
        }
        private void Send(long length)
        {
            Send(BitConverter.GetBytes(length));
        }
        public void Send(bool val)
        {
            Send(Convert.ToByte(val));
        }

        private void Send(byte val)
        {
            Send(new byte[] { val});
        }

        public void Send(SocketMessageType messageType)
        {
            Send((byte)messageType);
        }

        public void SendText(SocketMessageType messageType, string text)
        {
            Send(messageType);
            SendText(text);
        }

        public void SendText(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            Send(buffer.Length);
            Send(buffer);
        }

        public void SendStream(Stream reader, long length)
        {
            var sent = 0L;
            while (sent < length)
            {
                if (!ClientSocket.Connected || CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var buffer = new byte[Math.Min(length - sent, ChunkSize)];
                reader.Read(buffer, 0, buffer.Length);
                try
                {
                    ClientSocket.Send(buffer);
                }
                catch (Exception ex)
                {
                    Hub?.Logger.Error(ex.Message);
                    return;
                }
                sent += buffer.Length;
            }
        }

        public bool SendFile(string name,
            string md5,
            string fileName,
            CancellationToken token = default)
        {
            Send(SocketMessageType.FileCheck);
            SendText(name);
            SendText(md5);
            Hub?.Logger.Debug($"Check File:{name}");
            var type = ReceiveMessageType();
            if (type == SocketMessageType.FileCheckResponse)
            {
                var shouldSendName = ReceiveText();
                var shouldSend = ReceiveBool();
                if (!shouldSend)
                {
                    Hub?.Logger.Debug($"Quicky Send :{name}");
                    var fileInfo = new FileInfo(fileName);
                    Hub?.EmitSend(name, fileName, fileName.Length, fileInfo.Length);
                    // 秒传
                    return true;
                    // SendFile(fileName, token);
                }
            }
            var chunkSize = FileChunkSize;
            using var reader = File.OpenRead(fileName);
            var length = reader.Length;
            if (length <= chunkSize)
            {
                Send(SocketMessageType.File);
                SendText(name);
                SendText(md5);
                Send(length);
                SendStream(reader, length);
                Hub?.EmitSend(name, fileName, length, length);
                Hub?.Logger.Debug($"File Send :{name}");
                return true;
            }
            var partItems = new List<string>();
            var i = 0;
            var startPos = 0L;
            var endPos = 0L;
            while (endPos < length)
            {
                if (!ClientSocket.Connected || token.IsCancellationRequested || 
                    CancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                var partName = $"{md5}_{i}";
                Send(SocketMessageType.FilePart);
                SendText(partName);
                SendText(name);
                var partLength = Math.Min(length - startPos, chunkSize);
                endPos = startPos + partLength;
                SendText($"{startPos}-{endPos}/{length}");
                Send(partLength);
                SendStream(reader, partLength);
                partItems.Add(partName);
                i++;
                Hub?.EmitSend(name, fileName, endPos, length);
                Hub?.Logger.Debug($"File Send Part :{name}[{startPos}-{endPos}]");
                startPos = endPos;
            }
            Send(SocketMessageType.FileMerge);
            SendText(name);
            SendText(md5);
            SendText(string.Join(",", partItems));
            Hub?.EmitSend(name, fileName, length, length);
            Hub?.Logger.Debug($"File Send Merge :{name}");
            return true;
        }


        public void ReceiveFile(string folder, bool overwrite,
            CancellationToken token = default)
        {
            while (true)
            {
                if (!ClientSocket.Connected || token.IsCancellationRequested
                    || CancellationToken.IsCancellationRequested)
                {
                    Hub?.Close(this);
                    return;
                }
                var type = ReceiveMessageType();
                if (type == SocketMessageType.PreClose)
                {
                    Hub?.Logger.Debug("Receive Complete");
                    Hub?.Close(this);
                    return;
                }
                else if (type == SocketMessageType.FileCheck)
                {
                    var fileName = ReceiveText();
                    var md5 = ReceiveText();
                    var shouldSend = CheckFile(folder, fileName, md5);
                    Send(SocketMessageType.FileCheckResponse);
                    SendText(fileName);
                    Send(shouldSend);
                    Hub?.Logger.Debug($"Receive Check: {fileName}->{shouldSend}");
                    if (!shouldSend)
                    {
                        var location = Path.Combine(folder, fileName);
                        var fileInfo = new FileInfo(location);
                        Hub?.EmitReceive(fileName, location, fileInfo.Length, fileInfo.Length);
                    }
                    continue;
                }
                else if (type == SocketMessageType.FileCheckResponse)
                {
                    var fileName = ReceiveText();
                    var shouldSend = ReceiveBool();
                    if (shouldSend)
                    {
                        // SendFile(fileName, token);
                    }
                    continue;
                }
                else if (type == SocketMessageType.File)
                {
                    var fileName = ReceiveText();
                    var location = Path.Combine(folder, fileName);
                    if (File.Exists(fileName) && overwrite) {
                        Jump();
                        Jump();
                        Hub?.EmitReceive(fileName, location, 0,0);
                        Hub?.Logger.Debug($"Receive File Exist: {fileName}->{overwrite}");
                        continue;
                    }
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    var tempFile = Path.Combine(folder, $"_{md5}.cache");
                    using (var fs = File.Create(tempFile))
                    {
                        ReceiveStream(fs, length);
                    }
                    if (md5 != Disk.GetMD5(tempFile))
                    {
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        Hub?.EmitReceive(fileName, location, 0, 0);
                        File.Delete(tempFile);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    File.Move(tempFile, location);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    Hub?.EmitReceive(fileName, location, length, length);
                    continue;
                }
                else if (type == SocketMessageType.FileMerge)
                {
                    var fileName = ReceiveText();
                    var location = Path.Combine(folder, fileName);
                    var md5 = ReceiveText();
                    var partItems = ReceiveText().Split(',');
                    var cacheFile = Path.Combine(folder, $"_{md5}.cache");
                    // var length = CacheMergeFile(folder, cacheFile, partItems);
                    if (md5 != Disk.GetMD5(cacheFile))
                    {
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        File.Delete(cacheFile);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    File.Move(cacheFile, location);
                    var length = new FileInfo(location).Length;
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    Hub?.EmitReceive(fileName, location, length, length);
                    continue;
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = ReceiveText();
                    var fileName = ReceiveText();
                    var md5 = partName.Split('_')[0];
                    var location = Path.Combine(folder, fileName);
                    var rang = ReceiveText().Split(new char[] { '-', '/'});
                    var length = Convert.ToInt64(rang[2]);
                    var startPos = Convert.ToInt64(rang[0]);
                    var endPos = Convert.ToInt64(rang[1]);
                    var partLength = ReceiveContentLength();
                    var tempFile = Path.Combine(folder, $"_{md5}.cache");
                    using (var fs = File.OpenWrite(tempFile))
                    {
                        fs.SetLength(length);
                        fs.Seek(startPos, SeekOrigin.Begin);
                        ReceiveStream(fs, partLength);
                    }
                    Hub?.Logger.Debug($"Receive File Part: {fileName}[{startPos}-{endPos}]");
                    Hub?.EmitReceive(fileName, location, endPos, length);
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 验证是否需要传输
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <param name="md5"></param>
        /// <returns></returns>
        private bool CheckFile(string folder, string fileName, string md5)
        {
            var path = Path.Combine(folder, fileName);
            if (!File.Exists(path))
            {
                return true;
            }
            return Disk.GetMD5(path) != md5;
        }


        private long CacheMergeFile(string folder, string destFile, params string[] partFiles)
        {
            using var writer = File.Create(destFile);
            foreach (var item in partFiles)
            {
                var tempFile = Path.Combine(folder, $"_{item}.cache");
                if (File.Exists(item))
                {
                    return 0L;
                }
                using (var reader = File.Create(tempFile))
                {
                    reader.CopyTo(writer);
                }
                File.Delete(tempFile);
            }
            return writer.Length;
        }
        #endregion



        public void Dispose()
        {
            CancellationToken.Cancel();
            ClientSocket?.Close();
        }
    }
}
