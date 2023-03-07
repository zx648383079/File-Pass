 using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public SocketHub(ILogger logger)
        {
            Logger = logger;
            Udp = new UdpServer(this);
            Tcp = new TcpServer(this);
        }

        public ILogger Logger { get; private set; }
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
            var clientIp = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(clientIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(clientIp);
            } catch (Exception ex)
            {
                // 可能对方没有开启tcp
                App.Repository.Logger.Debug($"TCP[{ip}:{port}]: {ex.Message}");
                return null;
            }
            if (!socket.Connected)
            {
                return null;
            }
            return new SocketClient(socket, ip, port);
        }

        public async Task<SocketClient?> GetAsync(IUser user)
        {
            return await GetAsync(user.Ip, user.Port);
        }

        public async Task<SocketClient?> GetAsync(string ip, int port)
        {
            foreach (var item in ClientItems)
            {
                if (item.Ip != ip || item.Port != port)
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
                var client = Connect(ip, port);
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
            var buffer = RenderPack(new UserMessage() { Data = info }.Pack(),
                (byte)SocketMessageType.Ping, Convert.ToByte(true));
            foreach (var item in users)
            {
                if (item.Ip == ip)
                {
                    continue;
                }
                Udp.Ping(item.Ip, item.Port, buffer);
            }
        }

        public void ResponsePing(string ip, int port, IUser info)
        {
            var buffer = RenderPack(new UserMessage() { Data = info }.Pack(), 
                (byte)SocketMessageType.Ping, Convert.ToByte(false));
            Udp.Ping(ip, port, buffer);
        }

        public void Ping(string ip, int port, IUser info)
        {
            var buffer = RenderPack(new UserMessage() { Data = info }.Pack(),
                (byte)SocketMessageType.Ping, Convert.ToByte(true));
            Udp.Ping(ip, port, buffer);
        }
        /// <summary>
        /// 发送消息，自动转udp
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <returns>是否发送成功，udp无法判断默认返回true</returns>
        public async Task<bool> SendAsync(IUser user, SocketMessageType type,
            IMessagePack? message)
        {
            return await SendAsync(user.Ip, user.Port, type, true, message);
        }
        /// <summary>
        /// 响应消息，自动转udp
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <returns>是否发送成功，udp无法判断默认返回true</returns>
        public async Task<bool> ResponseAsync(IUser user, SocketMessageType type,
            IMessagePack message)
        {
            return await SendAsync(user.Ip, user.Port, type, false, message);
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
        public async Task<bool> SendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            var client = await GetAsync(ip, port);
            if (client == null)
            {
                return await UdpSendAsync(ip, port, type, isRequest, pack);
            }
            client.Send(type);
            client.Send(isRequest);
            if (pack is null)
            {
                return true;
            }
            return client.Send(pack);
        }

        public async Task<bool> UdpSendAsync(string ip, int port, SocketMessageType type, bool isRequest, IMessagePack? pack)
        {
            return await Udp.SendAsync(ip, port, type, isRequest, pack);
        }
        public async Task<bool> RequestAsync(string ip, int port, SocketMessageType type, IMessagePack pack)
        {
            return await SendAsync(ip, port, type, true, pack);
        }
        public async Task<bool> ResponseAsync(string ip, int port, SocketMessageType type, IMessagePack pack)
        {
            return await SendAsync(ip, port, type, false, pack);
        }

        #endregion

        internal MessageEventArg Emit(string ip, int port, byte[] buffer)
        {
            var type = (SocketMessageType)buffer[0];
            var isRequest = Convert.ToBoolean(buffer[1]);
            var pack = RenderUnpack(type);
            if (pack is not null)
            {
                pack.Unpack(buffer[2..]);
            }
            var arg = new MessageEventArg(type, isRequest, pack);
            MessageReceived?.Invoke(null, ip, port, arg);
            return arg;
        }

        internal MessageEventArg Emit(SocketClient client)
        {
            var arg = RenderReceivePack(client);
            if (arg.EventType == SocketMessageType.SpecialLine)
            {
                Change(client);
            }
            MessageReceived?.Invoke(client, client.Ip, client.Port, arg);
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
            var isRequest = type != SocketMessageType.Ip && client.ReceiveBool();
            var pack = RenderUnpack(type);
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
        /// 根据类型创建解包器
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IMessageUnpack? RenderUnpack(SocketMessageType? type)
        {
            return type switch
            {
                SocketMessageType.Ping or SocketMessageType.UserAddRequest or SocketMessageType.MessageUser => new UserMessage(),
                SocketMessageType.UserAddResponse => new BoolMessage(),
                SocketMessageType.MessageText or SocketMessageType.SpecialLine => new TextMessage(),
                SocketMessageType.MessageFile or SocketMessageType.MessageFolder or SocketMessageType.MessageSync => new FileMessage(),
                SocketMessageType.MessageAction or SocketMessageType.RequestSpecialLine => new ActionMessage(),
                SocketMessageType.Ip => new IpMessage(),
                _ => null,
            };
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
