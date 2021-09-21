using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.ViewModels
{
    public class MainViewModel: BindableBase
    {

        private ObservableCollection<string> ipItems = new ObservableCollection<string>();

        public ObservableCollection<string> IpItems
        {
            get => ipItems;
            set => Set(ref ipItems, value);
        }


        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();

        public ObservableCollection<FileItem> FileItems
        {
            get => fileItems;
            set => Set(ref fileItems, value);
        }


        public void AddFile(string name, string file, bool isClient = true)
        {
            FileItems.Add(new FileItem()
            {
                Name = name,
                Status = isClient  ? "准备发送" : "开始接收",
                FileName = file,
                Length = 0,
            });
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
            foreach (var item in FileItems)
            {
                if (item.Status == "发送中" || item.Status == "接收中")
                {
                    continue;
                }
                FileItems.Remove(item);
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
