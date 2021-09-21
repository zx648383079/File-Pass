using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Models
{
    public class FileItem : BindableBase
    {
        public string Name { get; set; }

        private string status = string.Empty;

        public string Status
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
            set => Set(ref progress, value);
        }

    }
}
