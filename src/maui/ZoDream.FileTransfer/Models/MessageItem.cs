﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class MessageItem: BindableObject
    {
        public bool IsSender { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        
    }

    internal class ActionMessageItem : MessageItem
    {
        public string Content { get; set; } = string.Empty;
    }

    internal class TextMessageItem : MessageItem
    {
        public string Content { get; set; } = string.Empty;
    }

    internal class FileMessageItem : MessageItem
    {
        public string FileName { get; set; }

        public long Size { get; set; }

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

        public string Location { get; set; }

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
            if (LastTime == DateTime.MinValue)
            {
                Speed = newProgress;
                return;
            }
            var now = DateTime.Now;
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