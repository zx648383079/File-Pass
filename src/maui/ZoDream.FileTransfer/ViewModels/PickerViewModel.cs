using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using ZoDream.FileTransfer.Controls;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.ViewModels
{
    public class PickerViewModel: ObservableObject
    {
        public PickerViewModel()
        {
            var items = Environment.GetLogicalDrives();
            foreach (var item in items)
            {
                FileItems.Add(new FilePickerOption()
                {
                    FileName = item,
                    Name = item[..(item.Length - 1)]
                });
            }
        }

        public StoragePicker Context { get; internal set; }

        public string Title { get; internal set; } = string.Empty;

        private ObservableCollection<FilePickerOption> fileItems = new ObservableCollection<FilePickerOption>();

        public ObservableCollection<FilePickerOption> FileItems {
            get => fileItems;
            set { 
                fileItems = value;
                OnPropertyChanged();
            }
        }


    }
}
