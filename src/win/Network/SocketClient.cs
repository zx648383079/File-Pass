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

        private bool connected = true;
        public bool Connected => connected && ClientSocket.Connected;


        #region 接受消息
        private SocketMessageType ReceiveMessageType()
        {
            var buffer = Receive(1);
            var type = (SocketMessageType)buffer[0];
            Hub?.Logger.Debug($"Message Type: {buffer[0]}[{type}]");
            return type;
        }

        private byte[] Receive(int length)
        {
            if (length <= 0 || length > FileChunkSize)
            {
                Hub?.Logger.Warning($"Receive pack too long: {length}");
                return Array.Empty<byte>();
            }
            var buffer = new byte[length];
            try
            {
                var index = 0;
                while (index < length)
                {
                    var size = ClientSocket.Receive(buffer, index,
                        length - index, SocketFlags.None);
                    index += size;
                }
                //if (size != length)
                //{
                //    Hub?.Logger.Error($"Receive Failure: {length}->{size}");
                //}
            }
            catch (Exception ex)
            {
                connected = false;
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
            return Encoding.UTF8.GetString(Receive((int)length));
        }

        public void ReceiveStream(Stream writer, long length)
        {
            var received = 0L;
            while (received < length)
            {
                var size = (int)Math.Min(length - received, ChunkSize);
                var buffer = Receive(size);
                if (buffer.Length < 1)
                {
                    return;
                }
                writer.Write(buffer, 0, size);
                received += size;
            }
        }

        public string ReceiveText(int length)
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
            var received = 0L;
            while (received < length)
            {
                var buffer = Receive((int)Math.Min(length - received, ChunkSize));
                if (buffer.Length < 1)
                {
                    return;
                }
                received += buffer.Length;
            }
        }
        #endregion

        #region 发送消息

        private void Send(byte[] buffer)
        {
            Send(buffer, buffer.Length);
        }

        private void Send(byte[] buffer, int length)
        {
            if (!Connected)
            {
                return;
            }
            try
            {
                var index = 0;
                while (index < length)
                {
                    var size = ClientSocket.Send(buffer, index, length - index, SocketFlags.None);
                    index += size;
                }
            }
            catch (Exception ex)
            {
                connected = false;
                Hub?.Logger.Error(ex.Message);
            }
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
                if (!Connected || CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var size = (int)Math.Min(length - sent, ChunkSize);
                var buffer = new byte[size];
                size = reader.Read(buffer, 0, size);
                if (size != buffer.Length)
                {
                    Hub?.Logger.Error("长度不对");
                }
                try
                {
                    Send(buffer, size);
                }
                catch (Exception ex)
                {
                    connected = false;
                    Hub?.Logger.Error(ex.Message);
                    return;
                }
                sent += size;
            }
        }

        public bool SendFile(string name,
            string md5,
            string fileName,
            long length,
            CancellationToken token = default)
        {
            Send(SocketMessageType.FileCheck);
            SendText(name);
            SendText(md5);
            Send(length);
            Hub?.Logger.Debug($"Check File:{name}");
            var type = ReceiveMessageType();
            if (type == SocketMessageType.FileCheckResponse)
            {
                var shouldSendName = ReceiveText();
                var shouldSend = ReceiveBool();
                if (!shouldSend)
                {
                    Hub?.Logger.Debug($"Quicky Send :{name}");
                    Hub?.EmitSend(name, fileName, length, length);
                    // 秒传
                    return true;
                    // SendFile(fileName, token);
                }
            }
            var chunkSize = FileChunkSize;
            using var reader = File.OpenRead(fileName);
            if (length <= chunkSize)
            {
                Send(SocketMessageType.File);
                SendText(name);
                SendText(md5);
                Send(length);
                SendStream(reader, length);
                type = ReceiveMessageType();
                Hub?.EmitSend(name, fileName, length, length);
                Hub?.Logger.Debug($"File Send :{name}");
                return type == SocketMessageType.Received;
            }
            var partItems = new List<string>();
            var i = 0;
            var startPos = 0L;
            var endPos = 0L;
            while (endPos < length)
            {
                if (!Connected || token.IsCancellationRequested || 
                    CancellationToken.IsCancellationRequested)
                {
                    Hub?.EmitSend(name, fileName, 0, 0);
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
                type = ReceiveMessageType();
                if (type != SocketMessageType.Received)
                {
                    Hub?.EmitSend(name, fileName, 0, 0);
                    Hub?.Close(this);
                    Hub?.Logger.Debug("Not Receive Reply");
                    return false;
                }
                startPos = endPos;
            }
            Send(SocketMessageType.FileMerge);
            SendText(name);
            SendText(md5);
            Send(length);
            SendText(string.Join(",", partItems));
            type = ReceiveMessageType();
            Hub?.EmitSend(name, fileName, length, length);
            Hub?.Logger.Debug($"File Send Merge :{name}");
            return type == SocketMessageType.Received;
        }


        public void ReceiveFile(string folder, bool overwrite,
            CancellationToken token = default)
        {
            var fileName = string.Empty;
            var location = string.Empty;
            while (true)
            {
                if (!Connected || token.IsCancellationRequested
                    || CancellationToken.IsCancellationRequested)
                {
                    Hub?.EmitReceive(fileName, location, 0, 0);
                    Hub?.Close(this);
                    Hub?.Logger.Debug("Socket Close");
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
                    fileName = ReceiveText();
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    location = Path.Combine(folder, fileName);
                    if (File.Exists(location) && !overwrite)
                    {
                        Send(SocketMessageType.FileCheckResponse);
                        SendText(fileName);
                        Send(false);
                        location = fileName = string.Empty;
                        Hub?.EmitReceive(fileName, location, 0, 0);
                        Hub?.Logger.Debug($"Receive File Exist: {fileName}->{overwrite}");
                        return;
                    }
                    var shouldSend = CheckFile(folder, fileName, md5);
                    Send(SocketMessageType.FileCheckResponse);
                    SendText(fileName);
                    Send(shouldSend);
                    Hub?.Logger.Debug($"Receive Check: {fileName}->{shouldSend}");
                    if (!shouldSend)
                    {
                        Hub?.EmitReceive(fileName, location, length, length);
                    }
                    continue;
                }else if (type == SocketMessageType.File)
                {
                    fileName = ReceiveText();
                    location = Path.Combine(folder, fileName);
                    if (File.Exists(location) && overwrite) {
                        Jump();
                        Jump();
                        Hub?.EmitReceive(fileName, location, 0,0);
                        Hub?.Logger.Debug($"Receive File Exist: {fileName}->{overwrite}");
                        location = fileName = string.Empty;
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
                        Send(SocketMessageType.ReceivedError);
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        Hub?.EmitReceive(fileName, location, 0, 0);
                        File.Delete(tempFile);
                        location = fileName = string.Empty;
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
#if NETCOREAPP3_0_OR_GREATER
                    File.Move(tempFile, location, overwrite);
#else
                    if (File.Exists(location)) 
                    {
                        File.Delete(location);
                    }
                    File.Move(tempFile, location);
#endif
                    Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    Hub?.EmitReceive(fileName, location, length, length);
                    location = fileName = string.Empty;
                    continue;
                }
                else if (type == SocketMessageType.FileMerge)
                {
                    fileName = ReceiveText();
                    location = Path.Combine(folder, fileName);
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    var partItems = ReceiveText().Split(',');
                    var cacheFile = Path.Combine(folder, $"_{md5}.cache");
                    // var length = CacheMergeFile(folder, cacheFile, partItems);
                    if (md5 != Disk.GetMD5(cacheFile))
                    {
                        Send(SocketMessageType.ReceivedError);
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        File.Delete(cacheFile);
                        location = fileName = string.Empty;
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
#if NETCOREAPP3_0_OR_GREATER
                    File.Move(cacheFile, location, overwrite);
#else
                    if (File.Exists(location)) 
                    {
                        File.Delete(location);
                    }
                    File.Move(cacheFile, location);
#endif
                    Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    Hub?.EmitReceive(fileName, location, length, length);
                    location = fileName = string.Empty;
                    continue;
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = ReceiveText();
                    fileName = ReceiveText();
                    var md5 = partName.Split('_')[0];
                    location = Path.Combine(folder, fileName);
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
                    Send(SocketMessageType.Received);
                    continue;
                }
                else
                {
                    Hub?.EmitReceive(fileName, location, 0, 0);
                    Hub?.Logger.Error($"Lose pack:{type}");
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
            connected = false;
            CancellationToken.Cancel();
            ClientSocket?.Close();
        }
    }
}
