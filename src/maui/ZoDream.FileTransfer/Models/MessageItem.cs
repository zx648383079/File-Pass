using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Network.Messages;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Models
{
    public class MessageItem: BindableObject
    {
        public bool IsSender { get; set; } = false;

        public string UserId { get; set; } = string.Empty;

        public string ReceiveId { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        private bool isSuccess;

        public bool IsSuccess
        {
            get { return isSuccess; }
            set { 
                isSuccess = value;
                OnPropertyChanged();
            }
        }

        public virtual string ToShortMessage()
        {
            return string.Empty;
        }

        public virtual void ReadFrom(IMessageUnpack data) 
        {

        }

        public virtual IMessagePack? WriteTo()
        {
            return null;
        }
    }

    public class ActionMessageItem : MessageItem
    {
        public string Content { get; set; } = string.Empty;

        public ActionMessageItem()
        {
            
        }

        public ActionMessageItem(SocketMessageType messageType)
        {
            Content = messageType switch
            {
                SocketMessageType.MessagePing => "拍拍你",
                SocketMessageType.Close => "连接已断开",
                _ => ""
            };
        }

        public override string ToShortMessage()
        {
            return Content;
        }
    }

    public class TextMessageItem : MessageItem
    {
        public string Content { get; set; } = string.Empty;

        public override string ToShortMessage()
        {
            return Content;
        }

        public override void ReadFrom(IMessageUnpack data)
        {
            if (data is TextMessage m)
            {
                Content = m.Data;
            }
        }

        public override IMessagePack? WriteTo()
        {
            return new TextMessage()
            {
                Data = Content
            };
        }
    }

    public class FileMessageItem : MessageItem
    {
        public string FileName { get; set; } = string.Empty;


        private long size;

        public long Size {
            get { return size; }
            set { 
                size = value;
                OnPropertyChanged();
            }
        }


        private long progress;

        public long Progress
        {
            get { return progress; }
            set {
                UpdateSpeed(value, progress);
                progress = value;
                OnPropertyChanged();
            }
        }

        private FileMessageStatus status = FileMessageStatus.None;

        public FileMessageStatus Status
        {
            get { return status; }
            set {
                status = value;
                OnPropertyChanged();
            }
        }

        public string Location { get; set; } = string.Empty;

        public string LocationFolder { get; set; } = string.Empty;

        private long speed = 0;

        public long Speed
        {
            get => speed;
            set
            {
                speed = value;
                OnPropertyChanged();
            }
        }

        private DateTime LastProgressTime = DateTime.MinValue;
        private DateTime LastSpeedTime = DateTime.MinValue;
        private long LastProgress = 0;

        public void UpdateSpeed(long newProgress, long oldProgress = 0)
        {
            var now = DateTime.Now;
            var speed = Disk.GetSpeed(now, newProgress, LastProgressTime, oldProgress);
            LastProgressTime = now;
            if (IsMoreThan(speed, Speed))
            {
                LastSpeedTime = now;
                Speed = speed;
                LastProgress = newProgress;
                return;
            }
            if ((now - LastSpeedTime).TotalSeconds < 5)
            {
                return;
            }
            speed = Disk.GetSpeed(now, newProgress, LastSpeedTime, LastProgress);
            LastSpeedTime = now;
            Speed = speed;
            LastProgress = newProgress;
        }
        /// <summary>
        /// 判断是否需要更新
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private bool IsMoreThan(long arg, long arg2)
        {
            if (arg == arg2)
            {
                return false;
            }
            if (arg <= 0 || arg <= 0)
            {
                return true;
            }
            var maxDiff = 10;
            return (arg > arg2 && arg2 * maxDiff < arg) ||
                (arg < arg2 && arg * maxDiff < arg2);
        }



        public FileMessageItem()
        {
            
        }

        public override string ToShortMessage()
        {
            return "[文件]";
        }

        public override void ReadFrom(IMessageUnpack data)
        {
            if (data is FileMessage m)
            {
                FileName = m.FileName;
                Size = m.Length;
                Id = m.MessageId;
            }
        }

        public override IMessagePack? WriteTo()
        {
            return new FileMessage()
            {
                FileName = FileName,
                Length = Size,
                MessageId = Id
            };
        }
    }

    public class FolderMessageItem : FileMessageItem
    {
        public string FolderName { get; set; } = string.Empty;
        public override string ToShortMessage()
        {
            return "[文件夹]";
        }

        public override void ReadFrom(IMessageUnpack data)
        {
            if (data is FileMessage m)
            {
                FolderName = m.FileName;
                Id = m.MessageId;
            }
        }

        public override IMessagePack? WriteTo()
        {
            return new FileMessage()
            {
                FileName = FolderName,
                MessageId = Id
            };
        }
    }

    public class SyncMessageItem : FolderMessageItem
    {

        
    }

    public class UserMessageItem: MessageItem
    {
        public IUser Data { get; set; }

        public override string ToShortMessage()
        {
            return $"[推荐用户]";
        }

        public override void ReadFrom(IMessageUnpack data)
        {
            if (data is UserMessage m)
            {
                Data = m.Data;
            }
        }

        public override IMessagePack? WriteTo()
        {
            return new UserMessage()
            {
                Data = Data,
                
            };
        }
    }

    public enum FileMessageStatus
    {
        None,
        Transferring,
        Success,
        Failure,
        Canceled,
    }
}
