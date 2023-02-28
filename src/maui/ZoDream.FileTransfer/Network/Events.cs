using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageReceivedEventHandler(SocketClient client, ISocketMessage message);

    public delegate void UsersUpdatedEventHandler();
    public delegate void NewUserEventHandler(UserInfoItem user);

    public delegate void NewMessageEventHandler(string userId, MessageItem message);

    public delegate void MessageTapEventHandler(object sender, MessageTapEventArg arg);
    
    public class MessageTapEventArg
    {
        public MessageTapEvent EventType { get; private set; }

        public MessageItem Data { get; private set; }

        public MessageTapEventArg(MessageItem message, MessageTapEvent eventType)
        {
            EventType = eventType;
            Data = message;
        }
    }

    public enum MessageTapEvent
    {
        None,
        Copy,
        Confirm,
        Cancel,
    }
}
