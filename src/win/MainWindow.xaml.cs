﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public MainViewModel ViewModel = new();
        private TransferClient? client;
        private TransferServer? server;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IpTb.Text = Ip.Get();
        }

        private void IpTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IpTb.Text))
            {
                return;
            }
            DistTb.Text = Regex.Replace(IpTb.Text, @"\d+$", "");
            ViewModel.Load(DistTb.Text, IpTb.Text);
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
                ShowNewFolderButton = true
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("请选择保存文件夹");
                ListenBtn.IsEnabled = true;
                return;
            }
            ViewModel.ServerMessage = "接收中...";
            var saveFolder = SaveFolderTb.Text = openFolderDialog.SelectedPath;
            ListenBtn.IsEnabled = false;
            if (server == null)
            {
                server = new TransferServer();
            }
            server.Open(ip, port);
            server.Listen(saveFolder, (FileInfoItem item) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ViewModel.AddFile(item, false);
                });
            }, (current, total, file) =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
                App.Current.Dispatcher.Invoke(() =>
                {
                    ViewModel.UpdateFile(file, current, total, false);
                });
            });
        }

        private void ChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            ChooseFolder();
        }

        private bool PreviewSend()
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
                client = new TransferClient();
            }
            client.Open(ip, port);
            ViewModel.ClientMessage = "准备发送...";
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
            SendFolder(openFolderDialog.SelectedPath);
        }

        private void SendFolder(string folder)
        {
            if (!PreviewSend())
            {
                return;
            }
            client!.SendFolder(folder, SendFileInit, SendFileProgress);
        }

        private void IpTb_GotFocus(object sender, RoutedEventArgs e)
        {
            IpTb.Focus();
            IpTb.SelectAll();
        }


        private void ClearMenu_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearFile();
            client?.Close();
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
            SendFiles(openFileDialog.FileNames);
        }

        private void SendFileInit(FileInfoItem item)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ChooseBtn.IsEnabled = false;
                ViewModel.AddFile(item, true);
            });
        }

        private void SendFileProgress(long current, long total, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return;
            }
            App.Current.Dispatcher.Invoke(() =>
            {
                if (current == total)
                {
                    ChooseBtn.IsEnabled = true;
                }
                ViewModel.UpdateFile(file, current, total, true);
            });
        }

        private void SendFiles(string[] files)
        {
            if (!PreviewSend())
            {
                return;
            }
            ChooseBtn.IsEnabled = false;
            client!.SendFiles(files, SendFileInit, SendFileProgress);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            client?.Close();
            server?.Close();
        }

        private void FileBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;
        }

        private void FileBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            var items = (IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop);
            if (items == null)
            {
                return;
            }
            if(!PreviewSend())
            {
                return;
            }
            client!.SendFileOrFolder(items, SendFileInit, SendFileProgress);
        }

        private void SaveFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = true
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            if (server == null)
            {
                server = new TransferServer();
            }
            SaveFolderTb.Text = server.SaveFolder = openFolderDialog.SelectedPath;
        }

        private void OverCb_Checked(object sender, RoutedEventArgs e)
        {
            if (server == null)
            {
                server = new TransferServer();
            }
            server.IsOverFile = true;
        }

        private void OverCb_Unchecked(object sender, RoutedEventArgs e)
        {
            if (server == null)
            {
                server = new TransferServer();
            }
            server.IsOverFile = false;
        }
    }
}