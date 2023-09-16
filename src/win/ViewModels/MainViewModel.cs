using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ZoDream.FileTransfer.Loggers;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.ViewModels
{
    public class MainViewModel: BindableBase, IDisposable
    {
        public MainViewModel()
        {
            ListenCommand = new RelayCommand(TapListen);
            SaveCommand = new RelayCommand(TapSaveFolder);
            DragFileCommand = new RelayCommand(TapDragFile);
            DragFolderCommand = new RelayCommand(TapDragFolder);
            ClearCommand = new RelayCommand(TapClearFile);
            ClientDoCommand = new RelayCommand(TapClientDo);
            ToggleCommand = new RelayCommand(TapToggle);
            Hub = new SocketHub(Logger);
            Hub.OnProgress += Hub_OnProgress;
            Hub.OnCompleted += Hub_OnCompleted;
            Hub.OnLinkChange += Hub_OnLinkChange;
            Logger.OnLog += Logger_OnLog;
            Task.Factory.StartNew(() => {
                ClientIp = Ip.Get();
                if (string.IsNullOrWhiteSpace(ClientIp))
                {
                    return;
                }
                SendIp = Regex.Replace(ClientIp, @"\d+$", "");
                Load(SendIp, ClientIp);
            });
        }

        const int DefaultPort = 63350;
        private readonly SocketHub Hub;
        public EventLogger Logger { get; private set; } = new EventLogger();

        public ICommand ListenCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand ClientDoCommand { get; private set; }
        public ICommand ToggleCommand { get; private set; }
        public ICommand DragFileCommand { get; private set; }
        public ICommand DragFolderCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }

        
        private bool isPassively = false;
        /// <summary>
        /// 判断是否是被动模式
        /// </summary>
        public bool IsPassively {
            get => isPassively;
            set {
                Set(ref isPassively, value);
                OnPropertyChanged(nameof(ToggleTip));
                OnPropertyChanged(nameof(SendText));
            }
        }


        private bool isNotListen = true;

        public bool IsNotListen {
            get => isNotListen;
            set => Set(ref isNotListen, value);
        }


        private string clientIp = string.Empty;

        public string ClientIp {
            get => clientIp;
            set => Set(ref clientIp, value);
        }

        private int clientPort = DefaultPort;

        public int ClientPort {
            get => clientPort;
            set => Set(ref clientPort, value);
        }

        private string sendIp = string.Empty;

        public string SendIp {
            get => sendIp;
            set => Set(ref sendIp, value);
        }

        private int sendPort = DefaultPort;

        public int SendPort {
            get => sendPort;
            set => Set(ref sendPort, value);
        }

        private string saveFolder = string.Empty;

        public string SaveFolder {
            get => saveFolder;
            set => Set(ref saveFolder, value);
        }

        private bool overwrite = true;

        public bool Overwrite {
            get => overwrite;
            set {
                Set(ref overwrite, value);
                Hub.Overwrite = value;
            }
        }

        public string ToggleTip => LocalizedLangExtension.GetString(IsPassively ? "toggleToTip" : "toggleTip");

        public string SendText => LocalizedLangExtension.GetString(IsPassively ? "linkTo" : "pickFile");


        private string linkedCount;

        public string LinkedCount {
            get => linkedCount;
            set => Set(ref linkedCount, value);
        }



        private ObservableCollection<string> ipItems = new();

        public ObservableCollection<string> IpItems
        {
            get => ipItems;
            set => Set(ref ipItems, value);
        }


        private ObservableCollection<FileItem> fileItems = new();

        public ObservableCollection<FileItem> FileItems
        {
            get => fileItems;
            set => Set(ref fileItems, value);
        }

        private CancellationTokenSource messageToken = new();
        private string serverMessage = string.Empty;

        public string ServerMessage
        {
            get => serverMessage;
            set => Set(ref serverMessage, value);
        }

        private string clientMessage = string.Empty;

        public string ClientMessage
        {
            get => clientMessage;
            set => Set(ref clientMessage, value);
        }

        public bool IsVerifySendAddress => Regex.IsMatch(SendIp, @"\d+\.\d+\.\d+\.\d") && SendPort >= 1000;

        public bool CanDropFile => (IsPassively && !IsNotListen) || (!IsPassively && IsVerifySendAddress);

        private void Logger_OnLog(string message, LogLevel level)
        {
            if (level > LogLevel.Debug)
            {
                ShowMessage(message);
            }
        }

        private void Hub_OnLinkChange()
        {
            LinkedCount = $"{Hub.LinkedCount}/{Hub.LinkCount}";
        }

        private void Hub_OnCompleted(string name, string fileName, bool isSuccess, bool isSend)
        {
            App.Current.Dispatcher.Invoke(() => {
                var item = GetOrAdd(name, fileName, isSend);
                if (item is null)
                {
                    return;
                }
                if (isSend)
                {
                    item.Status = isSuccess ? FileStatus.Sent : FileStatus.SendFailure;
                } else
                {
                    item.Status = isSuccess ? FileStatus.Received : FileStatus.ReceiveFailure;
                }
                UpdateMessage(isSend);
            });
        }

        private void Hub_OnProgress(string name, string fileName, long progress, long total, bool isSend)
        {
            App.Current.Dispatcher.Invoke(() => {
                var item = GetOrAdd(name, fileName, isSend);
                if (item is null)
                {
                    return;
                }
                item.Status = isSend ? FileStatus.Sending : FileStatus.Receiving;
                item.Length = total;
                item.Progress = progress;
            });
        }

        private void TapToggle(object? _)
        {
            IsPassively = !IsPassively;
        }

        private void TapClientDo(object? _)
        {
            if (!IsPassively)
            {
                TapDragFile(null);
                return;
            }
            if (!CheckSaveFolder())
            {
                return;
            }
            if (!IsVerifySendAddress)
            {
                MessageBox.Show(LocalizedLangExtension.GetString("remoteIpError"));
                return;
            }
            Hub.WorkFolder = SaveFolder;
            Hub.Overwrite = Overwrite;
            Hub.Add(SendIp, SendPort, true);
        }

        private bool CheckSaveFolder()
        {
            if (!string.IsNullOrWhiteSpace(SaveFolder))
            {
                return true;
            }
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = true
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show(LocalizedLangExtension.GetString("pickSaveFolderTip"));
                IsNotListen = true;
                return false;
            }
            SaveFolder = openFolderDialog.SelectedPath;
            return true;
        }

        private void TapListen(object? _)
        {
            if (string.IsNullOrWhiteSpace(ClientIp) || ClientPort < 1000)
            {
                MessageBox.Show(LocalizedLangExtension.GetString("clientIpError"));
                return;
            }
            if (!CheckSaveFolder())
            {
                return;
            }
            ServerMessage = LocalizedLangExtension.GetString("receivingTip");
            IsNotListen = false;
            Hub.WorkFolder = SaveFolder;
            Hub.Overwrite = Overwrite;
            Hub.Listen(ClientIp, ClientPort);
        }


        private void TapSaveFolder(object? _)
        {
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = false
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(SaveFolder))
                {
                    MessageBox.Show(LocalizedLangExtension.GetString("pickSaveFolderTip"));
                }
                return;
            }
            SaveFolder = openFolderDialog.SelectedPath;
            Hub.WorkFolder  = SaveFolder;
        }

        private void TapDragFile(object? _)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Multiselect = true,
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }
            DragFile(openFileDialog.FileNames);
        }



        private void TapDragFolder(object? _)
        {
            if (!CanDropFile)
            {
                MessageBox.Show(LocalizedLangExtension.GetString(IsPassively ?  "listenNotOpen" : "remoteIpError"));
                return;
            }
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = AppDomain.CurrentDomain.BaseDirectory,
                ShowNewFolderButton = false
            };
            if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            var folder = openFolderDialog.SelectedPath;
            Task.Factory.StartNew(() => {
                var items = Disk.GetAllFile(folder);
                DragFile(items);
            });
        }

        private void TapClearFile(object? _)
        {
            ClearFile();
            Hub.StopSend();
        }

        public FileItem? Get(string name, bool isSend)
        {
            for (int i = FileItems.Count - 1; i >= 0; i--)
            {
                var item = FileItems[i];
                if (item.Name != name)
                {
                    continue;
                }
                if ((isSend && item.Status >= FileStatus.ReadySend) 
                    || (!isSend && item.Status < FileStatus.ReadySend))
                {
                    return item;
                }
            }
            return null;
        }

        public FileItem? GetOrAdd(string name, string fileName, bool isSend)
        {
            var item = Get(name, isSend);
            if (item is not null)
            {
                return item;
            }
            item = new FileItem(name, fileName)
            {
                Status = isSend ? FileStatus.ReadySend : FileStatus.ReadyReceive
            };
            FileItems.Add(item);
            return item;
        }

        public int FileIndexOf(string file)
        {
            for (int i = 0; i < FileItems.Count; i++)
            {
                if (FileItems[i].FileName == file)
                {
                    return i;
                } 
            }
            return -1;
        }


        public void AddFile(string name, string file, bool isClient = true)
        {
            AddFile(new FileItem(name, file)
            {
                Status = isClient ? FileStatus.ReadySend : FileStatus.ReadyReceive,
                Length = 0,
            });
        }

        public void AddFile(FileInfoItem file, bool isClient = true)
        {
            AddFile(new FileItem(file.Name, file.FileName)
            {
                Status = isClient ? FileStatus.ReadySend : FileStatus.ReadyReceive,
                Length = file.Length,
            });
        }

        public void AddFile(FileItem file)
        {
            var i = FileIndexOf(file.FileName);
            if (i < 0)
            {
                FileItems.Add(file);
                return;
            }
            FileItems[i] = file;
        }

        public void UpdateFile(string file, long current, long total, bool isClient = true)
        {
            foreach (var item in FileItems)
            {
                if (item.FileName != file)
                {
                    continue;
                }
                if (total == 0)
                {
                    item.Status = isClient ? FileStatus.SendFailure : FileStatus.ReceiveFailure;
                    break;
                }
                if (current < 0)
                {
                    item.Status = isClient ? FileStatus.SendIgnore : FileStatus.ReceiveIgnore;
                    break;
                }
                if (total == current)
                {
                    item.Status = isClient ? FileStatus.Sent : FileStatus.Received;
                } else
                {
                    item.Status = isClient ? FileStatus.Sending : FileStatus.Receiving;
                }
                if (total > 0)
                {
                    item.Length = total;
                }
                item.Progress = current;
                break;
            }
            UpdateMessage(isClient);
        }

        public void UpdateMessage(bool isClient = true)
        {
            var total = 0;
            var finish = 0;
            var failure = 0;
            foreach (var item in FileItems)
            {
                if (isClient && 
                    (item.Status >= FileStatus.ReadySend || item.Status <= FileStatus.SendFailure)
                    )
                {
                    total++;
                    if (item.Status == FileStatus.Sent)
                    {
                        finish++;
                    } else if (item.Status == FileStatus.SendFailure)
                    {
                        failure++;
                    }
                } else if (!isClient &&
                    (item.Status >= FileStatus.ReadyReceive || item.Status <= FileStatus.ReceiveFailure))
                {
                    total++;
                    if (item.Status == FileStatus.Received)
                    {
                        finish++;
                    }
                    else if (item.Status == FileStatus.ReceiveFailure)
                    {
                        failure++;
                    }
                }
            }
            var message = string.Format(
                LocalizedLangExtension.GetString("messageTip"), finish, failure, total);
            if (isClient)
            {
                ClientMessage = message;
            } else
            {
                ServerMessage = message;
            }
        }

        public void ClearFile()
        {
            for (int i = FileItems.Count - 1; i >= 0; i--)
            {
                var item = FileItems[i];
                if (item.Status == FileStatus.Sending || item.Status == FileStatus.Receiving)
                {
                    continue;
                }
                FileItems.RemoveAt(i);
            }
        }

        public void DragFile(IEnumerable<string> items)
        {
            if (!CanDropFile)
            {
                MessageBox.Show(LocalizedLangExtension.GetString(IsPassively ? "listenNotOpen" : "remoteIpError"));
                return;
            }
            foreach (var item in items)
            {
                DragFile(item);
            }
        }

        public void DragFile(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Exists)
            {
                DragFile(new FileInfoItem(fileInfo.Name, fileName, fileInfo.Name, fileInfo.Length));
                return;
            }
            var folderInfo = new DirectoryInfo(fileName);
            if (folderInfo.Exists)
            {
                Task.Factory.StartNew(() => {
                    DragFile(Disk.GetAllFile(folderInfo.FullName, folderInfo.Name + "\\"));
                });
                return;
            }
            
        }

        public void DragFile(FileInfoItem item)
        {
            App.Current.Dispatcher.Invoke(() => {
                var fileItem = GetOrAdd(item.RelativeFile, item.FileName, true);
                if (fileItem == null)
                {
                    return;
                }
                fileItem.FileInfo = item;
                fileItem.Status = FileStatus.ReadySend;
                fileItem.Length = item.Length;
            });
            Task.Factory.StartNew(() => {
                if (string.IsNullOrEmpty(item.Md5))
                {
                    item.Md5 = Disk.GetMD5(item.FileName);
                }
                if (IsPassively)
                {
                    Hub.SendFileAsync(item);
                } else
                {
                    Hub.SendFileAsync(SendIp, SendPort, item);
                }
                
            });
        }

        public void DragFile(IEnumerable<FileInfoItem> items)
        {
            foreach (var item in items)
            {
                DragFile(item);
            }
        }

        public async void Load(string baseIp, string existIp)
        {
            if (string.IsNullOrWhiteSpace(baseIp))
            {
                return;
            }
            var items = await Ip.AllAsync(baseIp, existIp);
            App.Current.Dispatcher.Invoke(() => {
                foreach (var item in items)
                {
                    IpItems.Add(item);
                }
            });
        }

        public void ShowMessage(string message)
        {
            messageToken.Cancel();
            messageToken = new CancellationTokenSource();
            var token = messageToken.Token;
            ClientMessage = message;
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(3000);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                ClientMessage = string.Empty;
            }, token);

        }

        public void Dispose()
        {
            Hub.Dispose();
        }
    }
}
