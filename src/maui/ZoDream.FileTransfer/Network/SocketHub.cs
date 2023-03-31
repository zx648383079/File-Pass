using System.Net;
using System.Net.Sockets;
using ZoDream.FileTransfer.Loggers;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network.Messages;

namespace ZoDream.FileTransfer.Network
{
    /// <summary>
    /// 消息传递类
    /// 消息格式 [1B:消息体类型][1B:是否时请求][8B:消息体长度][-消息体内容]
    /// </summary>
    public class SocketHub : IDisposable, IMessageSender
    {

        public SocketHub(ISocketProvider provider, ILogger logger)
        {
            Logger = logger;
            Provider = provider;
            Udp = new UdpServer(this);
            Tcp = new TcpServer(this);
        }

        public ILogger Logger { get; private set; }
        public ISocketProvider Provider { get; private set; }
        public UdpServer Udp { get; private set; }
        public TcpServer Tcp { get; private set; }
        private readonly IList<SocketClient> ClientItems = new List<SocketClient>();
        private readonly IList<SocketClient> SpecialItems = new List<SocketClient>();
        public event MessageReceivedEventHandler? MessageReceived;

        public void Add(SocketClient client)
        {
            if (client == null)
            {
                return;
            }
            ClientItems.Add(client);
            client.Hub = this;
            client.LoopReceive();
        }


        public static SocketClient? Connect(string ip, int port)
        {
            var socket = ConnectTCP(ip, port);
            if (socket == null)
            {
                return null; ;
            }
            return new SocketClient(socket, ip, port);
        }

        private static Socket? ConnectTCP(string ip, int port)
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                return null;
            }
            var clientIp = new IPEndPoint(address, port);
            var socket = new Socket(clientIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(clientIp);
            }
            catch (Exception ex)
            {
                // 可能对方没有开启tcp
                App.Repository.Logger.Debug($"TCP[{ip}:{port}]: {ex.Message}");
                return null;
            }
            if (!socket.Connected)
            {
                return null;
            }
            return socket;
        }

        public static SocketClient? Connect(IClientAddress token)
        {
            var socket = ConnectTCP(token.Ip, token.Port);
            if (socket == null)
            {
                return null; ;
            }
            return new SocketClient(socket, token);
        }

        public async Task<SocketClient?> GetAsync(IUser user)
        {
            return await GetAsync(user.Ip, user.Port);
        }

        public async Task<SocketClient?> GetAsync(string ip, int port)
        {
            return await GetAsync(Provider.GetToken(ip, port));
        }

        public async Task<SocketClient?> GetAsync(IClientAddress address)
        {
            var token = address is IClientToken o ? o : Provider.GetToken(address);
            foreach (var item in ClientItems)
            {
                if (item.Token is null || 
                    item.Token.Ip != token.Ip || 
                    item.Token.Port != token.Port || item.Token.Id != token.Id)
                {
                    continue;
                }
                if (!item.Connected)
                {
                    continue;
                }
                return item;
            }
            return await Task.Factory.StartNew(() => {
                var client = Connect(token);
                if (client == null)
                {
                    return null;
                }
                Add(client);
                client.SendIp(App.Repository.Option);
                return client;
            });
        }
        #region 发送

        /// <summary>
        /// 通知所有客户端上线了
        /// </summary>
        /// <param name="info"></param>
        public void Ping(IUser info)
        {
            var ip = Utils.Ip.GetIpInGroup();
            if (string.IsNullOrEmpty(ip))
            {
                return;
            }
            var baseIp = ip[..(ip.LastIndexOf('.') + 1)];
            var items = new List<IClientAddress>();
            for (var i = 1; i <= 255; i++)
            {
                var sendIp = baseIp + i;
                if (sendIp == ip)
                {
                    continue;
                }
                items.Add(new ClientAddress(sendIp, Udp.ListenPort));
            }
            Ping(items, info);
        }

        /// <summary>
        /// 通知所有好友上线了
        /// </summary>
        /// <param name="users"></param>
        /// <param name="info"></param>
        public void Ping(IEnumerable<IClientAddress> users, IUser info)
        {
            var ip = Utils.Ip.GetIpInGroup();
            var buffer = TypeMessage.Pack(SocketMessageType.Ping, true, new UserMessage() { Data = info }); ;
            foreach (var item in users)
            {
                if (item.Ip == ip)
                {
                    continue;
                }
                Udp.Ping(item.Ip, item.Port, buffer);
            }
        }


        public void Ping(string ip, int port, IUser info, bool isRequest = true)
        {
            var buffer = TypeMessage.Pack(SocketMessageType.Ping, isRequest, new UserMessage() { Data = info });
            Udp.Ping(ip, port, buffer);
        }

        public void Ping(IClientAddress address, IUser info, bool isRequest = true)
        {
            Ping(address.Ip, address.Port, info, isRequest);
        }


        /// <summary>
        /// 发送消息，自动转udp
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="type"></param>
        /// <param name="isRequest"></param>
        /// <param name="pack"></param>
        /// <returns>是否发送成功，udp无法判断默认返回true</returns>
        public async Task<bool> SendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            var client = await GetAsync(address);
            if (client == null)
            {
                return await UdpSendAsync(address, type, isRequest, pack);
            }
            return client.Send(type, isRequest, pack);
        }

        public async Task<bool> UdpSendAsync(IClientAddress address, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            return await Udp.SendAsync(address, type, isRequest, pack);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="pack"></param>
        /// <returns></returns>
        public async Task<bool> RequestAsync(IClientAddress address, SocketMessageType type, IMessagePack? pack)
        {
            return await SendAsync(address, type, true, pack);
        }
        /// <summary>
        /// 响应消息，自动转udp
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="pack"></param>
        /// <returns>是否发送成功，udp无法判断默认返回true</returns>
        public async Task<bool> ResponseAsync(IClientAddress address, SocketMessageType type, IMessagePack? pack)
        {
            return await SendAsync(address, type, false, pack);
        }

        #endregion

        internal MessageEventArg Emit(IClientAddress address, byte[] buffer)
        {
            var type = (SocketMessageType)buffer[0];
            var isRequest = false;
            var start = 1;
            if (MessageEventArg.HasRequest(type))
            {
                start = 2;
                isRequest = Convert.ToBoolean(buffer[1]);
            }
            var pack = MessageEventArg.RenderUnpack(type);
            pack?.Unpack(buffer[start..]);
            var arg = new MessageEventArg(type, isRequest, pack);
            MessageReceived?.Invoke(null, Provider.GetToken(address), arg);
            return arg;
        }

        internal MessageEventArg Emit(IClientAddress address, MessageEventArg arg)
        {
            MessageReceived?.Invoke(null, Provider.GetToken(address), arg);
            return arg;
        }


        internal MessageEventArg Emit(SocketClient client, SocketMessageType? type = null)
        {
            var arg = RenderReceivePack(client, type);
            if (arg.EventType == SocketMessageType.SpecialLine)
            {
                Change(client);
            }
            MessageReceived?.Invoke(client, client.Token, arg);
            return arg;
        }
        /// <summary>
        /// 切换成专线
        /// </summary>
        /// <param name="client"></param>
        public void Change(SocketClient client)
        {
            client.StopLoopReceive();
            ClientItems.Remove(client);
            SpecialItems.Add(client);
        }
        internal void Close(SocketClient client)
        {
            ClientItems.Remove(client);
            SpecialItems.Remove(client);
            client.Dispose();
        }

        public void Dispose()
        {
            foreach (var item in ClientItems)
            {
                item.Dispose();
            }
            Udp.Dispose();
            Tcp.Dispose();
        }

        public static MessageEventArg RenderReceivePack(SocketClient client)
        {
            return RenderReceivePack(client, null);
        }

        /// <summary>
        /// 解包一个类型
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static MessageEventArg RenderReceivePack(SocketClient client, SocketMessageType? type)
        {
            type ??= client.ReceiveMessageType();
            // 请注意有些事件是没有isRequest 这个参数的，
            var isRequest = false;
            if (MessageEventArg.HasRequest(type))
            {
                isRequest = client.ReceiveBool();
            }
            var pack = MessageEventArg.RenderUnpack(type);
            if (pack is IMessageUnpackStream o)
            {
                o.Unpack(client);
            }
            else
            {
                pack?.Unpack(client.ReceiveBuffer());
            }
            if (type == SocketMessageType.Ip && pack is IpMessage address)
            {
                client.Address = address;
            }
            return new MessageEventArg((SocketMessageType)type, isRequest, pack);
        }

        

        /// <summary>
        /// 自动跳过一些不需要的类型
        /// </summary>
        /// <param name="client"></param>
        /// <param name="wantTypeItems"></param>
        /// <returns></returns>
        public static SocketMessageType JumpUnknownType(SocketClient client, 
            params SocketMessageType[] wantTypeItems)
        {
            while (client.Connected)
            {
                var type = client.ReceiveMessageType();
                if (wantTypeItems.Contains(type))
                {
                    return type;
                }
                RenderReceivePack(client, type);
            }
            return SocketMessageType.Close;
        }

        public static byte[] RenderPack(byte[] data, params byte[] prepend)
        {
            var buffer = new byte[data.Length + prepend.Length];
            Buffer.BlockCopy(prepend, 0, buffer, 0, prepend.Length);
            Buffer.BlockCopy(data, 0, buffer, prepend.Length, data.Length);
            return buffer;
        }

    }
}
