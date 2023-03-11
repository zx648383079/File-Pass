using System;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Models
{
    public class FileItem : BindableBase
    {
        public string Name { get; set; }

        private FileStatus status = FileStatus.None;

        public FileStatus Status
        {
            get => status;
            set {
                if (status != value)
                {
                    UpdateSpeed(0);
                }
                Set(ref status, value);
            }
        }


        public string FileName { get; set; }


        private long length;

        public long Length
        {
            get => length;
            set => Set(ref length, value);
        }



        private long progress;

        public long Progress
        {
            get => progress;
            set
            {
                UpdateSpeed(value, progress);
                Set(ref progress, value);
            }
        }


        private long speed = 0;

        public long Speed
        {
            get => speed;
            set => Set(ref speed, value);
        }

        public FileInfoItem? FileInfo { get; set; }
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
            if (diff <= 0)
            {
                LastTime = now;
                Speed = 0;
                return;
            }
            var newSpeed = (long)Math.Ceiling(Math.Max(newProgress - oldProgress, 0) / diff);
            if (diff > 20 || Math.Abs(newSpeed - Speed) > Speed / 10)
            {
                LastTime = now;
                Speed = newSpeed;
            }
        }


        public FileItem(string name, string fileName)
        {
            Name = name;
            FileName = fileName;
        }
    }

    public enum FileStatus
    {
        None,
        ReadyReceive,
        Receiving,
        Received,
        ReceiveIgnore,
        ReceiveFailure,
        ReadySend,
        Sending,
        Sent,
        SendIgnore,
        SendFailure,
    }
}
