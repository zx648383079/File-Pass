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

        public string FileName { get; set; }

        public string RelativeFile { get; set; }

        public long Length { get; set; }

        public string Md5 { get; set; } = string.Empty;

        public FileInfoItem(string name, string fileName, string relativeFile, long size = 0)
        {
            Name = name;
            FileName = fileName;
            RelativeFile = relativeFile;
            Length = size;
        }
    }
}
