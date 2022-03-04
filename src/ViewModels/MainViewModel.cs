using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


        private ObservableCollection<FileItem> fileItems = new();

        public ObservableCollection<FileItem> FileItems
        {
            get => fileItems;
            set => Set(ref fileItems, value);
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
            var i = FileIndexOf(file);
            var item = new FileItem(name, file)
            {
                Status = isClient ? "准备发送" : "开始接收",
                Length = 0,
            };
            if (i < 0)
            {
                FileItems.Add(item);
                return;
            }
            FileItems[i] = item;
        }

        public void UpdateFile(string file, long current, long total, bool isClient = true)
        {
            var label = isClient ? "发送" : "接收";
            foreach (var item in FileItems)
            {
                if (item.FileName != file)
                {
                    continue;
                }
                if (total == 0)
                {
                    item.Status = $"{label}失败";
                    break;
                }
                item.Status = label + (total == current ? "成功" : "中");
                if (total > 0)
                {
                    item.Length = total;
                }
                item.Progress = current;
                break;
            }
        }

        public void ClearFile()
        {
            for (int i = FileItems.Count - 1; i >= 0; i--)
            {
                var item = FileItems[i];
                if (item.Status == "发送中" || item.Status == "接收中")
                {
                    continue;
                }
                FileItems.RemoveAt(i);
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
    }
}
