using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class FileInfoItem
    {
        public string Name { get; set; }

        public string File { get; set; }

        public string RelativeFile { get; set; }

        public FileInfoItem(string name, string fileName, string relativeFile)
        {
            Name = name;
            File = fileName;
            RelativeFile = relativeFile;
        }
    }
}
