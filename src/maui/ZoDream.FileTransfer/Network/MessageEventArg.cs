using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Network.Messages;

namespace ZoDream.FileTransfer.Network
{
    public class MessageEventArg
    {

        public SocketMessageType EventType { get; private set; }

        public IMessageUnpack? Data { get; private set; }

        public bool IsRequest { get; private set; }

        public MessageEventArg(SocketMessageType type, IMessageUnpack? data):
            this(type, true, data)
        {
        }

        public MessageEventArg(SocketMessageType type, bool isRequest, IMessageUnpack? data)
        {
            EventType = type;
            Data = data;
            IsRequest = isRequest;
        }
        /// <summary>
        /// 判断那些消息类型有请求响应之分
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasRequest(SocketMessageType? type)
        {
            return type switch
            {
                SocketMessageType.Ip or SocketMessageType.SpecialLine or SocketMessageType.Null
                or SocketMessageType.RequestSpecialLine 
                or SocketMessageType.PreClose or SocketMessageType.Received 
                or SocketMessageType.ReceivedError or SocketMessageType.FileCheck 
                or SocketMessageType.FileCheckResponse or SocketMessageType.File 
                or SocketMessageType.FileMerge or SocketMessageType.FilePart 
                or SocketMessageType.FileDelete or SocketMessageType.FileRename => false,
                _ => true
            };
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
    }
}
