using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Network;

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

        private FileMessageStatus status;

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

        private DateTime LastTime = DateTime.MinValue;

        public void UpdateSpeed(long newProgress, long oldProgress = 0)
        {
            var now = DateTime.Now;
            if (LastTime == DateTime.MinValue)
            {
                Speed = newProgress;
                LastTime = now;
                return;
            }
            var diff = (now - LastTime).TotalSeconds;
            LastTime = now;
            if (diff <= 0)
            {
                Speed = 0;
                return;
            }
            Speed = (long)Math.Ceiling((newProgress - oldProgress) / diff);
        }

        public FileMessageItem()
        {
            
        }

        public override string ToShortMessage()
        {
            return "[文件]";
        }
    }

    public class FolderMessageItem : FileMessageItem
    {
        public string FolderName { get; set; } = string.Empty;
        public override string ToShortMessage()
        {
            return "[文件夹]";
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
