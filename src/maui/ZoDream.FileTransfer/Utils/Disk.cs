using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            files.AddRange(fileInfo.Select(nextFile => new FileInfoItem(nextFile.Name, nextFile.FullName, relativeFile + nextFile.Name, nextFile.Length)));
            return files;
        }

        public static Task<IList<FileInfoItem>> GetAllFileAsync(string dir, string relativeFile = "")
        {
            return Task.Factory.StartNew(() => GetAllFile(dir, relativeFile));
        }

        public static string FormatSize(long size)
        {
            var len = size.ToString().Length;
            if (len < 4)
            {
                return $"{size}B";
            }
            if (len < 7)
            {
                return Math.Round(Convert.ToDouble(size / 1024d), 2) + "KB";
            }
            if (len < 10)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024), 2) + "MB";
            }
            if (len < 13)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024), 2) + "GB";
            }
            if (len < 16)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024), 2) + "TB";
            }
            return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024 / 1024), 2) + "PB";
        }

        public static string GetMD5(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
            {
                return string.Empty;
            }
            using var fs = new FileStream(fileName, FileMode.Open);
            return GetMD5(fs);
        }

        public static string GetMD5(Stream fs)
        {
            var md5 = MD5.Create();
            var res = md5.ComputeHash(fs);
            var sb = new StringBuilder();
            foreach (var b in res)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

    }
}
