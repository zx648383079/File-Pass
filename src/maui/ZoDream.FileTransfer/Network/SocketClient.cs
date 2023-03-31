using Microsoft.Maui.Storage;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network.Messages;

namespace ZoDream.FileTransfer.Network
{
    public class SocketClient : IDisposable
    {
        public readonly int ChunkSize = 500;
        public readonly int FileChunkSize = 100 * 1024;
        public IClientToken? Token { get; private set; }
        public SocketHub? Hub { get; set; }
        private bool IsLoopReceive = false;
        private readonly Socket ClientSocket;
        private CancellationTokenSource ReceiveToken = new();
        private readonly CancellationTokenSource SendToken = new();

        public SocketClient(Socket socket)
        {
            ClientSocket = socket;
        }

        public SocketClient(Socket socket, string ip, int port): this(socket)
        {
            Token = new ClientToken(ip, port);
        }

        public SocketClient(Socket socket, IClientAddress token) : this(socket)
        {
            Address = token;
        }

        private bool connected = true;
        public bool Connected => connected && ClientSocket.Connected;
        public IClientAddress? Address 
        {
            get {
                return Token;
            }
            set {
                if (value == null)
                {
                    Token = null;
                    return;
                }
                if (value is IClientToken o)
                {
                    Token = new ClientToken(o.Ip, o.Port, o.Id);
                    return;
                }
                Token = new ClientToken(value.Ip, value.Port);
            }
        }

        public void LoopReceive()
        {
            IsLoopReceive = true;
            ReceiveToken?.Cancel();
            ReceiveToken = new CancellationTokenSource();
            var token = ReceiveToken.Token;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        IsLoopReceive = false;
                        return;
                    }
                    if (!Connected)
                    {
                        IsLoopReceive = false;
                        Hub?.Close(this);
                        return;
                    }
                    Hub?.Emit(this);
                }
            }, token);
        }

        public void StopLoopReceive()
        {
            ReceiveToken?.Cancel();
            ReceiveToken = new CancellationTokenSource();
        }

        #region 接受消息

        private byte[] Receive(int length)
        {
            if (length <= 0 || length > FileChunkSize)
            {
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
            }
            catch (Exception ex)
            {
                connected = false;
                Hub?.Logger.Error(ex.Message);
            }
            return buffer;
        }

        public SocketMessageType ReceiveMessageType()
        {
            var buffer = Receive(1);
            return (SocketMessageType)buffer[0];
        }

        public long ReceiveContentLength()
        {
            var buffer = Receive(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public string ReceiveText()
        {
            var length = ReceiveContentLength();
            return ReceiveText((int)length);
        }

        public byte[] ReceiveBuffer()
        {
            var length = ReceiveContentLength();
            return Receive((int)length);
        }

        public void ReceiveStream(Stream writer,long length)
        {
            var rate = length;
            while (rate > 0)
            {
                var buffer = Receive((int)Math.Min(rate, ChunkSize));
                if (buffer.Length < 1)
                {
                    return;
                }
                writer.Write(buffer, 0, buffer.Length);
                rate -= buffer.Length;
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
        /// 等待获取下一条信息
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IMessageUnpack? Receive(SocketMessageType messageType)
        {
            if (IsLoopReceive)
            {
                // 处于循环中无法获取
                return null;
            }
            while (true)
            {
                var message = Hub?.Emit(this);
                if (message == null)
                {
                    return null;
                }
                if (message.EventType == messageType)
                {
                    return message.Data;
                }
                if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 只接收一个文件
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="overwrite"></param>
        /// <param name="onProgress"></param>
        /// <param name="onCompleted"></param>
        /// <param name="token"></param>
        public void ReceiveFile(string folder,
            bool overwrite = true,
            FileProgressEventHandler? onProgress = null,
            FileCompletedEventHandler? onCompleted = null,
            CancellationToken token = default)
        {
            var fileName = string.Empty;
            var location = string.Empty;
            var storage = App.Repository.Storage;
            while (true)
            {
                if (!Connected || token.IsCancellationRequested
                    || SendToken.IsCancellationRequested)
                {
                    onCompleted?.Invoke(fileName, location, false);
                    return;
                }
                var type = ReceiveMessageType();
                if (type == SocketMessageType.Ready)
                {
                    // 询问是否准备好了
                    var isRequest = ReceiveBool();
                    if (isRequest)
                    {
                        SendReady(false);
                    }
                    continue;
                }
                if (type == SocketMessageType.PreClose)
                {
                    Hub?.Logger.Debug("Receive Complete");
                    connected = false;
                    Hub?.Close(this);
                    return;
                }
                else if (type == SocketMessageType.FileCheck)
                {
                    fileName = ReceiveText();
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    location = Path.Combine(folder, fileName);
                    var shouldSend = storage.CheckFile(location, md5, overwrite);
                    Send(SocketMessageType.FileCheckResponse);
                    SendText(fileName);
                    Send(shouldSend);
                    Hub?.Logger.Debug($"Receive Check: {fileName}->{shouldSend}");
                    if (!shouldSend)
                    {
                        onCompleted?.Invoke(fileName, location, 
                            File.Exists(location) && !overwrite ? false : null);
                        return;
                    }
                    continue;
                }
                else if (type == SocketMessageType.File)
                {
                    fileName = ReceiveText();
                    location = Path.Combine(folder, fileName);
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    using (var fs = storage.CacheWriter(md5))
                    {
                        ReceiveStream(fs, length);
                    }
                    onProgress?.Invoke(fileName, location, length, length);
                    if (md5 != storage.CacheFileMD5(md5))
                    {
                        Send(SocketMessageType.ReceivedError);
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        onCompleted?.Invoke(fileName, location, false);
                        storage.CacheRemove(md5);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    storage.CacheMove(md5, location);
                    Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    onCompleted?.Invoke(fileName, location, true);
                    return;
                }
                else if (type == SocketMessageType.FileMerge)
                {
                    fileName = ReceiveText();
                    location = Path.Combine(folder, fileName);
                    var md5 = ReceiveText();
                    var length = ReceiveContentLength();
                    Jump();
                    // var partItems = ReceiveText().Split(',');
                    if (md5 != storage.CacheFileMD5(md5))
                    {
                        Hub?.Logger.Debug($"Receive File Failure: {fileName}->{md5}");
                        storage.CacheRemove(md5);
                        Send(SocketMessageType.ReceivedError);
                        onCompleted?.Invoke(fileName, location, false);
                        return;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(location)!);
                    storage.CacheMove(md5, location);
                    Send(SocketMessageType.Received);
                    Hub?.Logger.Debug($"Receive File Complete: {fileName}->{length}");
                    onCompleted?.Invoke(fileName, location, true);
                    return;
                }
                else if (type == SocketMessageType.FilePart)
                {
                    var partName = ReceiveText();
                    fileName = ReceiveText();
                    var md5 = partName.Split('_')[0];
                    location = Path.Combine(folder, fileName);
                    var rang = ReceiveText().Split(new char[] { '-', '/' });
                    var length = Convert.ToInt64(rang[2]);
                    var startPos = Convert.ToInt64(rang[0]);
                    var endPos = Convert.ToInt64(rang[1]);
                    var partLength = ReceiveContentLength();
                    using (var fs = storage.CacheWriter(md5, true))
                    {
                        fs.SetLength(length);
                        fs.Seek(startPos, SeekOrigin.Begin);
                        ReceiveStream(fs, partLength);
                    }
                    Hub?.Logger.Debug($"Receive File Part: {fileName}[{startPos}-{endPos}]");
                    onProgress?.Invoke(fileName, location, endPos, length);
                    Send(SocketMessageType.Received);
                    continue;
                }
                else
                {
                    onCompleted?.Invoke(fileName, location, false);
                    Hub?.Logger.Error("Lose pack");
                    return;
                }
            }
        }


        /// <summary>
        /// 跳过
        /// </summary>
        public void Jump()
        {
            Jump(ReceiveContentLength());
        }

        public void Jump(SocketMessageType type)
        {
            if (Hub == null)
            {
                SocketHub.RenderReceivePack(this, type);
                return;
            }
            Hub.Emit(this, type);
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

        public void Send(byte[] buffer)
        {
            Send(buffer, buffer.Length);
        }
        public void Send(long length)
        {
            Send(BitConverter.GetBytes(length));
        }

        public void Send(bool val)
        {
            Send(Convert.ToByte(val));
        }

        public void Send(byte val)
        {
            Send(new byte[] { val});
        }

        public void Send(SocketMessageType messageType)
        {
            Send((byte)messageType);
        }

        public bool SendIp(IClientAddress address)
        {
            if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
            {
                return false;
            }
            Send(SocketMessageType.Ip);
            new IpMessage() { 
                Data = address
            }.Pack(this);
            return true;
        }
        public bool Send(SocketMessageType type, bool isRequest, IMessagePack? message)
        {
            Send(type);
            if (MessageEventArg.HasRequest(type))
            {
                Send(isRequest);
            }
            if (message == null)
            {
                return true;
            }
            return Send(message);
        }

        public bool Send(IMessagePack message)
        {
            if (!ClientSocket.Connected || ReceiveToken.IsCancellationRequested)
            {
                return false;
            }
            if (message is IMessagePackStream o)
            {
                o.Pack(this);
            } else
            {
                Send(message.Pack());
            }
            return true;
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
                if (!ClientSocket.Connected || SendToken.IsCancellationRequested)
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
                sent += buffer.Length;
            }
        }

        public void SendReady(bool isRequest = true)
        {
            Send(SocketMessageType.Ready);
            Send(isRequest);
        }

        /// <summary>
        /// 循环询问是否准备好了吗？
        /// </summary>
        /// <returns></returns>
        public bool AreYouReady()
        {
            while (true)
            {
                if (!Connected || SendToken.IsCancellationRequested)
                {
                    return false;
                }
                SendReady(true);
                var type = ReceiveMessageType();
                if (type == SocketMessageType.Ready)
                {
                    var isRequest = ReceiveBool();
                    if (!isRequest)
                    {
                        return true;
                    }
                }
                Jump(type);
            }
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="md5"></param>
        /// <param name="fileName"></param>
        /// <param name="length"></param>
        /// <param name="onProgress"></param>
        /// <param name="onCompleted"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool SendFile(string name, 
            string md5, 
            string fileName,
            long length,
            FileProgressEventHandler? onProgress = null,
            FileCompletedEventHandler? onCompleted = null,
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
                    onCompleted?.Invoke(name, fileName, null);
                    // 秒传
                    return true;
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
                onProgress?.Invoke(name, fileName, length, length);
                Hub?.Logger.Debug($"File Send :{name}");
                type = ReceiveMessageType();
                onCompleted?.Invoke(name, fileName, type == SocketMessageType.Received);
                return type == SocketMessageType.Received;
            }
            var partItems = new List<string>();
            var i = 0;
            var startPos = 0L;
            var endPos = 0L;
            while (endPos < length)
            {
                if (!Connected || token.IsCancellationRequested ||
                    SendToken.IsCancellationRequested)
                {
                    onCompleted?.Invoke(name, fileName, false);
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
                onProgress?.Invoke(name, fileName, endPos, length);
                Hub?.Logger.Debug($"File Send Part :{name}[{startPos}-{endPos}]");
                type = ReceiveMessageType();
                if (type != SocketMessageType.Received)
                {
                    onCompleted?.Invoke(name, fileName, false);
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
            onProgress?.Invoke(name, fileName, length, length);
            Hub?.Logger.Debug($"File Send Merge :{name}");
            type = ReceiveMessageType();
            onCompleted?.Invoke(name, fileName, type == SocketMessageType.Received);
            return type == SocketMessageType.Received;
        }


        #endregion

        public void Dispose()
        {
            connected = false;
            ReceiveToken?.Cancel();
            SendToken?.Cancel();
            ClientSocket?.Close();
        }
    }
}
