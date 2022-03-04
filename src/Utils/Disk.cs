using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Utils
{
    public static class Disk
    {
        /// <summary>
        /// 遍历文件夹
        /// </summary>
        /// <param name="dir"></param>
        public static IList<FileInfoItem> GetAllFile(string dir, string relativeFile = "")
        {
            var files = new List<FileInfoItem>();
            if (string.IsNullOrWhiteSpace(dir))
            {
                return files;
            }
            var theFolder = new DirectoryInfo(dir);
            var dirInfo = theFolder.GetDirectories();
            //遍历文件夹
            foreach (var nextFolder in dirInfo)
            {
                files.AddRange(GetAllFile(nextFolder.FullName, relativeFile + nextFolder.Name + "\\"));
            }

            var fileInfo = theFolder.GetFiles();
            //遍历文件
            files.AddRange(fileInfo.Select(nextFile => new FileInfoItem(nextFile.Name, nextFile.FullName, relativeFile + nextFile.Name)));
            return files;
        }
    }
}
