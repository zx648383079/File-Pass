using Microsoft.Win32;
using NetFwTypeLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace File_Pass
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public delegate void RefreshUI();
        private int splitSize = 1024 * 16;//切片尺寸，单位字节
        private string ip;
        private int port;

        private string saveFolder; // 保存的文件夹
        private bool isLoading = false; // 发送任务是否进行中
        private int threadCount = 1; // 进程数量

        private ObservableCollection<FileItem> FileItems = new ObservableCollection<FileItem>();

        public MainWindow()
        {
            InitializeComponent();
            FileBox.ItemsSource = FileItems;
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>本机IP地址</returns>
        public static string GetLocalIP()
        {
            try
            {
                var HostName = Dns.GetHostName(); //得到主机名
                var IpEntry = Dns.GetHostEntry(HostName);
                var ips = IpEntry.AddressList;
                for (int i = ips.Length - 1; i >= 0; i--)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (ips[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ips[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IpTb.Text = GetLocalIP();
            DistTb.Text = Regex.Replace(IpTb.Text, @"\d+$", "");
            LoadAllIp(DistTb.Text, IpTb.Text);
        }

        private void ListenBtn_Click(object sender, RoutedEventArgs e)
        {
            ip = IpTb.Text;
            port = Convert.ToInt32(ListenPortTb.Text);
            if (string.IsNullOrWhiteSpace(ip) || port < 1000)
            {
                MessageBox.Show("本机ip或端口错误");
                return;
            }
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = false
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("请选择保存文件夹");
                ListenBtn.IsEnabled = true;
                return;
            }
            NotifyTb.Text = "开始监听";
            saveFolder = openFolderDialog.SelectedPath;
            ListenBtn.IsEnabled = false;
            NetFwAddPorts("LargeFile", port, "TCP");
            ConcurrentQueue<TcpClient> cqs = new ConcurrentQueue<TcpClient>();
            Task listenTask = new Task(() =>
            {
                TcpListener tl = new TcpListener(IPAddress.Parse(ip), port);
                tl.Start();
                while (true)
                {
                    TcpClient tc = tl.AcceptTcpClient();
                    cqs.Enqueue(tc);
                }
            });
            listenTask.Start();
            Task receiveTask = new Task(() =>
            {
                while (true)
                {
                    if (cqs.Count > 0)
                    {
                        for (int i = 0; i < threadCount; i++)
                        {
                            TcpClient tc;
                            while (!cqs.TryDequeue(out tc))
                            {
                                Thread.Sleep(1);
                            }
                            Task.Factory.StartNew(() =>
                            {
                                ReceiveFile(tc);
                            });
                            if (cqs.Count < 1)
                            {
                                Dispatcher.Invoke(new RefreshUI(() =>
                                {
                                    NotifyTb.Text = "接收任务已分配完成，监听中";
                                }));
                                break;
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            });
            receiveTask.Start();
        }

        private void ReceiveFile(TcpClient tc)
        {
            NetworkStream ns;
            try
            {
                ns = tc.GetStream();

                byte[] fileInfo = new byte[268];//文件名最长260字节，文件尺寸最大8字节
                for (int i = 0; i < fileInfo.Length; i++)
                {
                    fileInfo[i] = Convert.ToByte(ns.ReadByte());
                }

                byte[] fileLengthBytes = fileInfo.Take(8).ToArray();

                byte[] fileNameBytes = fileInfo.Skip(8).ToArray();
                string fileName = Encoding.UTF8.GetString(fileNameBytes).TrimEnd(char.MinValue);

                var filePath = saveFolder + "\\" + fileName;
                var fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                var fileItem = new FileItem()
                {
                    Name = fileName,
                    Status = "开始接收",
                    FileName = filePath,
                    Length = fileLength
                };
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    FileItems.Add(fileItem);
                }));
                //接收路径目前写死
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    ns.CopyTo(fs);

                    if (fs.Length == fileLength)
                    {
                        Dispatcher.Invoke(new RefreshUI(() =>
                        {
                            fileItem.Status = "接收成功";
                        }));
                        ns.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    MessageBox.Show("出现错误：" + ex.Message + "堆栈信息：" + ex.StackTrace);
                }));
            }
            finally
            {
                try
                {
                    tc.Close();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new RefreshUI(() =>
                    {
                        MessageBox.Show("出现错误：" + ex.Message + "堆栈信息：" + ex.StackTrace);
                    }));
                }
            }
        }

        private void ChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            ip = DistTb.Text;
            port = Convert.ToInt32(SendPortTb.Text);
            if (string.IsNullOrWhiteSpace(ip) || port < 1000)
            {
                MessageBox.Show("目标ip或端口错误");
                return;
            }
            ChooseFolder();

        }
        /// <summary>
        /// 选择文件夹发送
        /// </summary>
        private void ChooseFolder()
        {
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = false
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("请选择保存文件夹");
                ChooseBtn.IsEnabled = true;
                return;
            }
            ChooseBtn.IsEnabled = false;
            var items = GetAllFile(openFolderDialog.SelectedPath);
            SendFiles(items);
        }

        /// <summary>
        /// 发送多个文件
        /// </summary>
        /// <param name="items"></param>
        private void SendFiles(List<FileItem> items)
        {
            if (items.Count < 1)
            {
                MessageBox.Show("无文件");
                ChooseBtn.IsEnabled = true;
                return;
            }
            foreach (var item in items)
            {
                item.Status = "等待中";
                FileItems.Add(item);
            }
            NotifyTb.Text = $"已选择 {items.Count} 个文件，准备发送";
            isLoading = true;
            ThreadPool.QueueUserWorkItem(w =>
            {
                Parallel.ForEach(items, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = threadCount
                }, fileItem =>
                {
                    sendFile(fileItem);
                });
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    ChooseBtn.IsEnabled = true;
                    NotifyTb.Text = "全部文件已发送成功";
                    isLoading = false;
                }));
            }, null);

            // NetFwAddPorts("LargeFile", port, "TCP");
        }

        /// <summary>
        /// 发送一个文件
        /// </summary>
        /// <param name="fileItem"></param>
        private void sendFile(FileItem fileItem)
        {
            Dispatcher.Invoke(new RefreshUI(() =>
            {
                ChooseBtn.IsEnabled = false;
                fileItem.Status = "开始发送";
            }));
            TcpClient tc = null;
            NetworkStream ns = null;
            try
            {
                tc = new TcpClient();
                tc.Connect(ip, port);

                ns = tc.GetStream();
                //在基于 Windows 的平台上，路径必须少于 248 个字符，且文件名必须少于 260 个字符。
                using (FileStream fileStream = new FileStream(fileItem.FileName, FileMode.Open, FileAccess.Read))
                {
                    long fileLength = fileStream.Length;
                    Dispatcher.Invoke(new RefreshUI(() =>
                    {
                        fileItem.Length = fileLength;
                    }));
                    string fileName = fileItem.Name;
                    if (fileLength > splitSize)
                    {
                        int splitCount = Convert.ToInt32(fileLength / splitSize);
                        int lastCount = Convert.ToInt32(fileLength % splitSize);

                        //发送文件和包信息
                        byte[] fileFlag = new byte[268];
                        byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                        for (int i = 0; i < fileLengthBytes.Length + fileNameBytes.Length; i++)
                        {
                            if (i < 8)
                            {
                                fileFlag[i] = fileLengthBytes[i];
                            }
                            else
                            {
                                fileFlag[i] = fileNameBytes[i - 8];
                            }
                        }

                        ns.Write(fileFlag, 0, fileFlag.Length);

                        for (int i = 0; i < splitCount; i++)
                        {
                            byte[] content = new byte[splitSize];

                            fileStream.Position = Convert.ToInt64(i) * Convert.ToInt64(splitSize);

                            fileStream.Read(content, 0, splitSize);

                            ns.Write(content, 0, content.Length);
                        }

                        if (lastCount > 0)
                        {
                            byte[] lastContent = new byte[lastCount];

                            fileStream.Position = Convert.ToInt64(splitCount) * Convert.ToInt64(splitSize);

                            fileStream.Read(lastContent, 0, lastCount);

                            ns.Write(lastContent, 0, lastContent.Length);
                        }

                        Dispatcher.Invoke(new RefreshUI(() =>
                        {
                            // MessageBox.Show(fileName + "发送完毕");
                            fileItem.Status = "发送成功";
                            ChooseBtn.IsEnabled = true;
                        }));
                    }
                    else
                    {
                        //发送文件和包信息
                        byte[] fileFlag = new byte[268];
                        byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                        for (int i = 0; i < fileLengthBytes.Length + fileNameBytes.Length; i++)
                        {
                            if (i < 8)
                            {
                                fileFlag[i] = fileLengthBytes[i];
                            }
                            else
                            {
                                fileFlag[i] = fileNameBytes[i - 8];
                            }
                        }

                        ns.Write(fileFlag, 0, fileFlag.Length);

                        byte[] content = new byte[fileLength];

                        fileStream.Read(content, 0, Convert.ToInt32(fileLength));

                        ns.Write(content, 0, content.Length);

                        Dispatcher.Invoke(new RefreshUI(() =>
                        {
                            fileItem.Status = "发送成功";
                            ChooseBtn.IsEnabled = true;
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    MessageBox.Show("出现错误：" + ex.Message + "堆栈信息：" + ex.StackTrace);
                }));
            }
            finally
            {
                try
                {
                    ns.Close();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new RefreshUI(() =>
                    {
                        MessageBox.Show("出现错误：" + ex.Message + "堆栈信息：" + ex.StackTrace);
                        fileItem.Status = "发送失败";
                        ChooseBtn.IsEnabled = true;
                    }));
                }

                try
                {
                    tc.Close();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(new RefreshUI(() =>
                    {
                        MessageBox.Show("出现错误：" + ex.Message + "堆栈信息：" + ex.StackTrace);
                    }));
                }
            }
        }

        /// <summary>
        /// 添加防火墙例外端口
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="port">端口</param>
        /// <param name="protocol">协议(TCP、UDP)</param>
        public static void NetFwAddPorts(string name, int port, string protocol)
        {
            INetFwMgr netFwMgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));

            INetFwOpenPort objPort = (INetFwOpenPort)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwOpenPort"));

            objPort.Name = name;
            objPort.Port = port;
            if (protocol.ToUpper() == "TCP")
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            }
            else
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
            }
            objPort.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            objPort.Enabled = true;

            bool exist = false;
            foreach (INetFwOpenPort mPort in netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts)
            {
                if (objPort == mPort)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist) netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts.Add(objPort);
        }

        private void IpTb_GotFocus(object sender, RoutedEventArgs e)
        {
            IpTb.Focus();
            IpTb.SelectAll();
        }

        /// <summary>
        /// 遍历文件夹
        /// </summary>
        /// <param name="dir"></param>
        public static List<FileItem> GetAllFile(string dir)
        {
            var files = new List<FileItem>();
            if (string.IsNullOrWhiteSpace(dir))
            {
                return files;
            }
            var theFolder = new DirectoryInfo(dir);
            var dirInfo = theFolder.GetDirectories();
            //遍历文件夹
            foreach (var nextFolder in dirInfo)
            {
                files.AddRange(GetAllFile(nextFolder.FullName));
            }

            var fileInfo = theFolder.GetFiles();
            //遍历文件
            files.AddRange(fileInfo.Select(nextFile => new FileItem()
            {
                Name = nextFile.Name,
                FileName = nextFile.FullName,
                Length = nextFile.Length,
            }));
            return files;
        }

        private void ClearMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            FileItems.Clear();
        }

        private void ChooseFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            ChooseFolder();
        }

        private void ChooseFileMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            var openFileDialog = new OpenFileDialog()
            {
                Multiselect = true,
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            var items = new List<FileItem>();
            for (int i = 0; i < openFileDialog.FileNames.Length; i++)
            {
                items.Add(new FileItem()
                {
                    FileName = openFileDialog.FileNames[i],
                    Name = openFileDialog.SafeFileNames[i]
                });
            }
            SendFiles(items);
        }

        /// <summary>
        /// 获取局域网的其他ip
        /// </summary>
        /// <param name="baseIp"></param>
        /// <param name="exsits"></param>
        private void LoadAllIp(string baseIp, string exsits)
        {
            for (int i = 1; i <= 255; i++)
            {
                var ip = baseIp + i;
                if (ip == exsits)
                {
                    continue;
                }
                var ping = new Ping();
                ping.PingCompleted += Ping_PingCompleted;
                ping.SendAsync(ip, 2000, null);
            }
        }

        private void Ping_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Reply.Status == IPStatus.Success)
            {
                DistTb.Items.Add(e.Reply.Address.ToString());
            }
        }
    }

    public class  FileItem: INotifyPropertyChanged
    {
        public string Name { get; set; }

        private string status;

        public string Status
        {
            get { return status; }
            set { 
                status = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Status"));
                }
            }
        }


        public string FileName { get; set; }

        public long Length { get; set; }

        private long progress;

        public long Progress
        {
            get { return progress; }
            set {
                progress = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Progress"));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
