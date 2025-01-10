namespace ZoDream.Shared.Net
{
    public delegate void MessageReceivedEventHandler(SocketClient? client, IClientToken? token, MessageEventArg arg);

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

    /// <summary>
    /// 文件传输进度
    /// </summary>
    /// <param name="name">传输的文件名</param>
    /// <param name="fileName">传输的文件路径</param>
    /// <param name="progress">进度</param>
    /// <param name="total">文件内容长度</param>
    public delegate void FileProgressEventHandler(string name, string fileName, long progress, long total);
    /// <summary>
    /// 文件传输完成
    /// </summary>
    /// <param name="name">传输的文件名</param>
    /// <param name="fileName">传输的文件路径</param>
    /// <param name="isSuccess">null 表示秒传，false传输失败，传输成功</param>
    public delegate void FileCompletedEventHandler(string name, string fileName, bool? isSuccess);

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
