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
        private bool IsMoreThan(long arg,  long arg2)
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
