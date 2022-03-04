using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Network
{
    public class TransferClient
    {
        public string Ip { get; private set; } = "127.0.0.1";

        public int Port { get; private set; }

        public int ThreadCount { get; set; } = 1;

        private readonly int splitSize = 1024 * 16;

        private CancellationTokenSource cancellationToken = new();

        public void Open(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public void Close()
        {
            cancellationToken.Cancel();
        }

        public void Send()
        {

        }

        public void SendFile(string file, Action<string, string, string> init, Action<long, long, string> progress)
        {
            var fileName = Path.GetFileName(file);
            init?.Invoke(fileName, fileName, file);
            SendFile(fileName, file, progress);
        }

        public void SendFiles(IEnumerable<FileInfoItem> files, Action<string, string, string> init, Action<long, long, string> progress)
        {
            foreach (var item in files)
            {
                init?.Invoke(item.Name, item.RelativeFile, item.File);
            }
            ThreadPool.QueueUserWorkItem(w =>
            {
                Parallel.ForEach(files, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = ThreadCount
                }, fileItem =>
                {
                    SendFile(fileItem.RelativeFile, fileItem.File, progress);
                });
            }, null);
        }

        public void SendFiles(IEnumerable<string> files, Action<string, string, string> init, Action<long, long, string> progress)
        {
            SendFiles(files.Select(file =>
            {
                var name = Path.GetFileName(file);
                return new FileInfoItem(name, file, name);
            }).ToList(), init, progress);
        }

        public void SendFolder(string folder, Action<string, string, string> init, Action<long, long, string> progress)
        {
            SendFiles(Disk.GetAllFile(folder, Path.GetFileName(folder) + "\\"), init, progress);
        }

        public void SendFileOrFolder(IEnumerable<string> files, Action<string, string, string> init, Action<long, long, string> progress)
        {
            var items = new List<FileInfoItem>();
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.Directory) == 0)
                {
                    items.Add(new FileInfoItem(info.Name, file, info.Name));
                    continue;
                }
                items.AddRange(Disk.GetAllFile(file, Path.GetFileName(file) + "\\"));
            }
            SendFiles(items, init, progress);
        }

        private void SendFile(string fileName, string file, Action<long, long, string> progress)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            TcpClient? tc = null;
            NetworkStream? ns = null;
            try
            {
                tc = new TcpClient();
                tc.Connect(Ip, Port);

                ns = tc.GetStream();
                //在基于 Windows 的平台上，路径必须少于 248 个字符，且文件名必须少于 260 个字符。
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    long fileLength = fileStream.Length;
                    progress?.Invoke(0, fileLength, file);
                    ns.Write(BitConverter.GetBytes(fileLength), 0, 8);
                    byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                    ns.Write(BitConverter.GetBytes(Convert.ToInt64(nameBytes.Length)), 0, 8);
                    ns.Write(nameBytes, 0, nameBytes.Length);
                    long start = 0;
                    var size = splitSize;
                    while (start < fileLength)
                    {
                        var next = start + size;
                        if (next > fileLength)
                        {
                            size = Convert.ToInt32(fileLength - start);
                            next = fileLength;
                        }
                        byte[] content = new byte[size];
                        fileStream.Position = start;
                        fileStream.Read(content, 0, size);
                        ns.Write(content, 0, size);
                        progress?.Invoke(next, fileLength, file);
                        start = next;
                    }

                }
            }
            catch (Exception)
            {
                progress(0, 0, file);
            }
            finally
            {
                try
                {
                    ns?.Close();
                }
                catch (Exception)
                {
                    progress(0, 0, file);
                }

                try
                {
                    tc?.Close();
                }
                catch (Exception)
                {
                    progress(0, 0, file);
                }
            }
        }

        
    }
}
