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

namespace FilePass
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public delegate void RefreshUI();

        private ObservableCollection<FileItem> FileItems = new ObservableCollection<FileItem>();
        private PassServer server;
        private PassClient client;

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
            var ip = IpTb.Text;
            var port = Convert.ToInt32(ListenPortTb.Text);
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
            var saveFolder = openFolderDialog.SelectedPath;
            ListenBtn.IsEnabled = false;
            if (server == null)
            {
                server = new PassServer();
            }
            server.Open(ip, port);
            server.Listen(saveFolder, (name, _, file) =>
            {
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    FileItems.Add(new FileItem()
                    {
                        Name = name,
                        Status = "开始接收",
                        FileName = file,
                        Length = 0,
                    });
                }));
            }, (current, total, file) =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    foreach (var item in FileItems)
                    {
                        if (item.FileName != file)
                        {
                            continue;
                        }
                        if (total == 0)
                        {
                            item.Status = "接受失败";
                            break;
                        }
                        item.Status = total == current ? "接收成功" : "接收中";
                        if (total > 0)
                        {
                            item.Length = total;
                        }
                        item.Progress = current;
                        break;
                    }
                }));
            });
            
        }


        private void ChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            ChooseFolder();

        }

        private bool previewSend()
        {
            var ip = DistTb.Text;
            var port = Convert.ToInt32(SendPortTb.Text);
            if (string.IsNullOrWhiteSpace(ip) || port < 1000)
            {
                MessageBox.Show("目标ip或端口错误");
                return false;
            }
            if (client == null)
            {
                client = new PassClient();
            }
            client.Open(ip, port);
            return true;
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
            if (!previewSend())
            {
                return;
            }
            client.SendFolder(openFolderDialog.SelectedPath, (name, _, file) =>
            {
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    ChooseBtn.IsEnabled = false;
                    FileItems.Add(new FileItem()
                    {
                        Name = name,
                        Status = "准备发送",
                        FileName = file,
                        Length = 0,
                    });
                }));
            }, (current, total, file) =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    foreach (var item in FileItems)
                    {
                        if (current == total)
                        {
                            ChooseBtn.IsEnabled = true;
                        }
                        if (item.FileName != file)
                        {
                            continue;
                        }
                        if (total == 0)
                        {
                            item.Status = "发送失败";
                            break;
                        }
                        item.Status = total == current ? "发送成功" : "发送中";
                        if (total > 0) {
                            item.Length = total;
                        }
                        item.Progress = current;
                        break;
                    }
                }));
            });
        }

        private void IpTb_GotFocus(object sender, RoutedEventArgs e)
        {
            IpTb.Focus();
            IpTb.SelectAll();
        }


        private void ClearMenu_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in FileItems)
            {
                if (item.Status == "发送中" || item.Status == "接收中")
                {
                    continue;
                }
                FileItems.Remove(item);
            }
        }

        private void ChooseFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            ChooseFolder();
        }

        private void ChooseFileMenu_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Multiselect = true,
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            if (!previewSend())
            {
                return;
            }
            ChooseBtn.IsEnabled = false;
            client.SendFiles(openFileDialog.FileNames, (name, _, file) =>
            {
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    FileItems.Add(new FileItem()
                    {
                        Name = name,
                        Status = "准备发送",
                        FileName = file,
                        Length = 0,
                    });
                }));
            }, (current, total, file) =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
                Dispatcher.Invoke(new RefreshUI(() =>
                {
                    foreach (var item in FileItems)
                    {
                        if (item.FileName != file)
                        {
                            continue;
                        }
                        if (total == 0)
                        {
                            item.Status = "发送失败";
                            break;
                        }
                        item.Status = total == current ? "发送成功" : "发送中";
                        if (total > 0)
                        {
                            item.Length = total;
                        }
                        item.Progress = current;
                        break;
                    }
                }));
            });

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

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            server?.Close();
            client?.Close();
        }
    }
    
}
