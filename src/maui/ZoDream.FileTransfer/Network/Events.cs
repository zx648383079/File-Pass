using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageReceivedEventHandler(SocketClient? client, string ip, int port, MessageEventArg arg);

    public delegate void UsersUpdatedEventHandler();
    public delegate void NewUserEventHandler(IUser user, bool isAddRequest = false);

    public delegate void NewMessageEventHandler(string userId, MessageItem message);

    public delegate void MessageUpdatedEventHandler(string messageId, 
        MessageTapEvent eventType, object? data);

    public delegate void MessageTapEventHandler(object sender, MessageTapEventArg arg);
    public delegate void MessageProgressEventHandler(string messageId, 
        string fileName, long progress, long total);
    public delegate void MessageCompletedEventHandler(string messageId,
        string fileName, bool isSuccess);

    public class MessageTapEventArg
    {
        public MessageTapEvent EventType { get; private set; }

        public MessageItem? Data { get; private set; }

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
        Withdraw, // 撤回消息
    }
}
