using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Controls;
using ZoDream.FileTransfer.Models;
using System;
using System.Text.RegularExpressions;

namespace ZoDream.FileTransfer.ViewModels
{
    public class PickerViewModel : ObservableObject
    {
        public PickerViewModel()
        {
            CloseCommand = new RelayCommand(TapClose);
            BackCommand = new AsyncRelayCommand(TapBackAsync);
            SelectedCommand = new AsyncRelayCommand<FilePickerOption>(TapSelectedAsync);
            _ = LoadFileAsync();
        }

        public StoragePicker Context { get; internal set; }

        public string Title { get; internal set; } = string.Empty;

        private List<string> Histories = new();

        private bool canBackable = false;

        public bool CanBackable {
            get { return canBackable; }
            set { 
                canBackable = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<FilePickerOption> fileItems = new ObservableCollection<FilePickerOption>();

        public ObservableCollection<FilePickerOption> FileItems {
            get => fileItems;
            set {
                fileItems = value;
                OnPropertyChanged();
            }
        }
        public ICommand SelectedCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand BackCommand { get; private set; }

        public FilePickerOption SelectedItem {
            get {
                foreach (var item in FileItems)
                {
                    if (!item.IsChecked)
                    {
                        continue;
                    }
                    if (
                        Context.IsFolderPicker != item.IsFolder)
                    {
                        continue;
                    }
                    return item;
                }
                return null;
            }
        }

        public IList<FilePickerOption> SelectedItems {
            get {
                var items = new List<FilePickerOption>();
                foreach (var item in FileItems)
                {
                    if (!item.IsChecked)
                    {
                        continue;
                    }
                    if (
                        Context.IsFolderPicker != item.IsFolder)
                    {
                        continue;
                    }
                    items.Add(item);
                }
                return items;
            }
        }

        public Task<bool> ShowAsync()
        {
            return Task.Factory.StartNew(() => {
                while (Context.IsOpen)
                {
                    
                }
                return SelectedItem is not null;
            });
        }

        private async Task TapSelectedAsync(FilePickerOption item)
        {
            if (item.IsFolder)
            {
                Histories.Add(item.Name);
                await LoadFileAsync();
                return;
            }
            if (Context.IsMultiple)
            {
                item.IsChecked = !item.IsChecked;
                return;
            }
            item.IsChecked = true;
            Context.IsOpen = false;
        }

        private void TapClose()
        {
            Context.IsOpen = false;
        }

        private async Task TapBackAsync()
        {
            if (Histories.Count > 0)
            {
                Histories.RemoveAt(Histories.Count - 1);
                await LoadFileAsync();
            }
        }

        private async Task LoadFileAsync()
        {
            FileItems.Clear();
            if (Histories.Count < 1)
            {
                Title = "设备和驱动器";
                CanBackable = false;
                LoadDrivers();
                return;
            }
            CanBackable = true;
            var path = Path.Combine(Histories.ToArray());
            Title = Histories.Last().Length > 20 ?
                Histories.Last()[..20] + "..." :
                path.Length > 20 ? string.Concat("...", path.AsSpan(path.Length - 20)) : path;
            var filter = Context.IsFolderPicker || string.IsNullOrWhiteSpace(Context.Filter) ? null : new Regex(Context.Filter);
            var items = await GetFileAsync(path, Context.IsFolderPicker,
                filter);
            foreach (var item in items)
            {
                FileItems.Add(item);
            }
        }
        
        private void LoadDrivers()
        {
            var items = Environment.GetLogicalDrives();
            foreach (var item in items)
            {
                FileItems.Add(new FilePickerOption()
                {
                    FileName = item,
                    Name = item[..(item.Length - 1)],
                    IsFolder = true,
                });
            }
        }

        private Task<List<FilePickerOption>> GetFileAsync(string folder, bool isFolder, Regex filter)
        {
            return Task.Factory.StartNew(() => {
                var files = new List<FilePickerOption>();
                var folders = new List<FilePickerOption>();
                var dir = new DirectoryInfo(folder);
                var items = isFolder ? dir.GetDirectories() : dir.GetFileSystemInfos();
                foreach (var i in items)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        folders.Add(new FilePickerOption()
                        {
                            Name = i.Name,
                            IsFolder = true,
                            FileName = i.FullName,
                        });
                        continue;
                    }
                    if (filter is not null && !filter.IsMatch(i.Name))
                    {
                        continue;
                    }
                    files.Add(new FilePickerOption()
                    {
                        Name = i.Name,
                        IsFolder = false,
                        FileName = i.FullName,
                    });
                }
                folders.AddRange(files);
                return folders;
            });
        }
    }
}
