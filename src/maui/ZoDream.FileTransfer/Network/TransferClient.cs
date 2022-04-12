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

    public delegate void InitFunc(FileInfoItem fileInfo);
    public delegate void ProgressFunc(long current, long total, string fileName);
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
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken = new CancellationTokenSource();
            }
        }

        public void Close()
        {
            cancellationToken.Cancel();
        }

        public void SendFile(string file, InitFunc init, ProgressFunc progress)
        {
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
            {
                return;
            }
            init?.Invoke(new FileInfoItem(fileInfo.Name, file, fileInfo.Name, fileInfo.Length));
            SendFile(fileInfo.Name, file, progress);
        }

        public void SendFiles(IEnumerable<FileInfoItem> files, InitFunc init, ProgressFunc progress)
        {
            foreach (var item in files)
            {
                init?.Invoke(item);
            }
            ThreadPool.QueueUserWorkItem(w =>
            {
                Parallel.ForEach(files, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = ThreadCount,
                    CancellationToken = cancellationToken.Token,
                }, fileItem =>
                {
                    SendFile(fileItem.RelativeFile, fileItem.File, progress);
                });
            }, null);
        }

        public void SendFiles(IEnumerable<string> files, InitFunc init, ProgressFunc progress)
        {
            SendFiles(files.Select(file =>
            {
                var name = Path.GetFileName(file);
                return new FileInfoItem(name, file, name);
            }).ToList(), init, progress);
        }

        public async void SendFolder(string folder, InitFunc init, ProgressFunc progress)
        {
            SendFiles(await Disk.GetAllFileAsync(folder, Path.GetFileName(folder) + "\\"), init, progress);
        }

        public async void SendFileOrFolder(IEnumerable<string> files, InitFunc init, ProgressFunc progress)
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
                items.AddRange(await Disk.GetAllFileAsync(file, Path.GetFileName(file) + "\\"));
            }
            SendFiles(items, init, progress);
        }

        private void SendFile(string fileName, string file, ProgressFunc progress)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            TcpClient tc = null;
            NetworkStream ns = null;
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
