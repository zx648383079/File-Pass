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
            set => Set(ref status, value);
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
