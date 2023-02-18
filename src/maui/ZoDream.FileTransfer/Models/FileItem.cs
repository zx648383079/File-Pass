using System;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Models
{
    public class FileItem : BindableObject
    {
        public string Name { get; set; }

        private FileStatus status = FileStatus.None;

        public FileStatus Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }


        public string FileName { get; set; }


        private long length;

        public long Length
        {
            get => length;
            set
            {
                length = value;
                OnPropertyChanged();
            }
        }



        private long progress;

        public long Progress
        {
            get => progress;
            set
            {
                // UpdateSpeed(value, progress);
                progress = value;
                OnPropertyChanged();
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
