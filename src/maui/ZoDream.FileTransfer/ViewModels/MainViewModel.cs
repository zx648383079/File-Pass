using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.ViewModels
{
    public class MainViewModel: BindableBase
    {

        private ObservableCollection<string> ipItems = new();

        public ObservableCollection<string> IpItems
        {
            get => ipItems;
            set => Set(ref ipItems, value);
        }


        private ObservableCollection<FileItem> sendFileItems = new();

        public ObservableCollection<FileItem> SendFileItems
        {
            get => sendFileItems;
            set => Set(ref sendFileItems, value);
        }

        private ObservableCollection<FileItem> receiveFileItems = new();

        public ObservableCollection<FileItem> ReceiveFileItems
        {
            get => receiveFileItems;
            set => Set(ref receiveFileItems, value);
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


        public int FileIndexOf(string file, bool isClient = true)
        {
            var items = isClient ? SendFileItems : ReceiveFileItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].FileName == file)
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
            }, isClient);
        }

        public void AddFile(FileInfoItem file, bool isClient = true)
        {
            AddFile(new FileItem(file.Name, file.File)
            {
                Status = isClient ? FileStatus.ReadySend : FileStatus.ReadyReceive,
                Length = file.Length,
            }, isClient);
        }

        public void AddFile(FileItem file, bool isClient = true)
        {
            var i = FileIndexOf(file.FileName, isClient);
            var items = isClient ? SendFileItems : ReceiveFileItems;
            if (i < 0)
            {
                items.Add(file);
                return;
            }
            items[i] = file;
        }

        public void UpdateFile(string file, long current, long total, bool isClient = true)
        {
            var items = isClient ? SendFileItems : ReceiveFileItems;
            foreach (var item in items)
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
            var fininsh = 0;
            var failure = 0;
            var items = isClient ? SendFileItems : ReceiveFileItems;
            foreach (var item in items)
            {
                if (isClient && 
                    (item.Status >= FileStatus.ReadySend || item.Status <= FileStatus.SendFailure)
                    )
                {
                    total++;
                    if (item.Status == FileStatus.Sent)
                    {
                        fininsh++;
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
                        fininsh++;
                    }
                    else if (item.Status == FileStatus.ReceiveFailure)
                    {
                        failure++;
                    }
                }
            }
            var message = $"成功{fininsh}/失败{failure}/共{total}";
            if (isClient)
            {
                ClientMessage = message;
            } else
            {
                ServerMessage = message;
            }
        }

        public void ClearFile(bool isClient = true)
        {
            var items = isClient ? SendFileItems : ReceiveFileItems;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item.Status == FileStatus.Sending || item.Status == FileStatus.Receiving)
                {
                    continue;
                }
                items.RemoveAt(i);
            }
        }

        public async void Load(string baseIp, string exsitIp)
        {
            if (string.IsNullOrWhiteSpace(baseIp))
            {
                return;
            }
            var items = await Ip.AllAsync(baseIp, exsitIp);
            foreach (var item in items)
            {
                IpItems.Add(item);
            }
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
    }
}
