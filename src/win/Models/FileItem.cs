using System;
using ZoDream.FileTransfer.Utils;
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
        private DateTime LastProgressTime = DateTime.MinValue;
        private DateTime LastSpeedTime = DateTime.MinValue;
        private long LastProgress = 0;

        public void UpdateSpeed(long newProgress, long oldProgress = 0)
        {
            var now = DateTime.Now;
            var speed = Disk.GetSpeed(now, newProgress, LastProgressTime, oldProgress);
            LastProgressTime = now;
            if (speed == 0 || Speed == 0)
            {
                LastSpeedTime = now;
                Speed = speed;
                LastProgress = newProgress;
                return;
            }
            if ((now - LastSpeedTime).TotalSeconds < 30)
            {
                return;
            }
            speed = Disk.GetSpeed(now, newProgress, LastSpeedTime, LastProgress);
            LastSpeedTime = now;
            Speed = speed;
            LastProgress = newProgress;
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
