using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Controls;

public partial class StoragePicker : ContentView
{
	public StoragePicker()
	{
        SelectedCommand = new RelayCommand<FilePickerOption>(TapSelected);
        InitializeComponent();
    }

    public bool IsFolderPicker {
		get { return (bool)GetValue(IsFolderPickerProperty); }
		set { SetValue(IsFolderPickerProperty, value); }
	}

	// Using a DependencyProperty as the backing store for IsFolderPicker.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty IsFolderPickerProperty =
        BindableProperty.Create(nameof(IsFolderPicker), typeof(bool), typeof(StoragePicker), false);

	public bool IsMultiple {
		get { return (bool)GetValue(IsMultipleProperty); }
		set { SetValue(IsMultipleProperty, value); }
	}

	// Using a DependencyProperty as the backing store for IsMultiple.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty IsMultipleProperty =
        BindableProperty.Create(nameof(IsMultiple), typeof(bool), typeof(StoragePicker), false);


    public bool IsCheckable {
        get { return (bool)GetValue(IsCheckableProperty); }
        set { SetValue(IsCheckableProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsMultiple.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty IsCheckableProperty =
        BindableProperty.Create(nameof(IsCheckable), typeof(bool), typeof(StoragePicker), false);



    /// <summary>
    /// 通过正则表达式筛选
    /// </summary>
    public string Filter {
		get { return (string)GetValue(FilterProperty); }
		set { SetValue(FilterProperty, value); }
	}

	// Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty FilterProperty =
        BindableProperty.Create("Filter", typeof(string), typeof(StoragePicker), string.Empty);




    public ICommand ConfirmCommand {
        get { return (ICommand)GetValue(ConfirmCommandProperty); }
        set { SetValue(ConfirmCommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ConfirmCommand.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ConfirmCommandProperty =
        BindableProperty.Create(nameof(ConfirmCommand), typeof(ICommand), typeof(StoragePicker), null);



    public bool CanBackable {
        get { return (bool)GetValue(CanBackableProperty); }
        set { SetValue(CanBackableProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsMultiple.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty CanBackableProperty =
        BindableProperty.Create(nameof(CanBackable), typeof(bool), typeof(StoragePicker), false);

    public string Title {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(StoragePicker), string.Empty);

    public bool IsOpen {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(StoragePicker),
            false, propertyChanged: (b, oldVal, newVal) => {
                (b as StoragePicker)?.ToggleOpen((bool)oldVal, (bool)newVal);
            });


    public IList<FilePickerOption> Items {
		get { return (IList<FilePickerOption>)GetValue(ItemsProperty); }
		set { SetValue(ItemsProperty, value); }
	}

	// Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<FilePickerOption>), typeof(StoragePicker), 
            new List<FilePickerOption>());

    private bool Result = false;
    private Grid InnerPanel;
    private List<string> Histories = new();
    public ICommand SelectedCommand { get; private set; }

    public FilePickerOption SelectedItem {
        get {
            if (Items is null)
            {
                return null;
            }
            foreach (var item in Items)
            {
                if (!item.IsChecked)
                {
                    continue;
                }
                if (
                    IsFolderPicker != item.IsFolder)
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
            if (Items is null)
            {
                return items;
            }
            foreach (var item in Items)
            {
                if (!item.IsChecked)
                {
                    continue;
                }
                if (
                    IsFolderPicker != item.IsFolder)
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
        Result = false;
        IsOpen = true;
        IsCheckable = IsMultiple || IsFolderPicker;
        if ((Items is null || Items.Count < 1) && Histories.Count == 0)
        {
            LoadFile();
        }
        return Task.Factory.StartNew(() => {
            while (!Result)
            {

            }
            return SelectedItem is not null;
        });
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InnerPanel = GetTemplateChild("PART_InnerPanel") as Grid;
        var yesBtn = GetTemplateChild("PART_YesBtn") as Button;
        var closeBtn = GetTemplateChild("PART_CloseBtn") as Button;
        var backBtn = GetTemplateChild("PART_BackBtn") as Button;
        if (yesBtn != null)
        {
            yesBtn.Clicked += YesBtn_Clicked;
        }
        if (closeBtn != null)
        {
            closeBtn.Clicked += CloseBtn_Clicked;
        }
        if (backBtn != null)
        {
            backBtn.Clicked += BackBtn_Clicked;
        }
    }
    private void ToggleOpen(bool oldVal, bool newVal)
    {

        if (newVal)
        {
            InnerPanel?.TranslateTo(0, 0, 500, Easing.SinIn);
        }
        else
        {
            InnerPanel?.TranslateTo(0, 500, 500, Easing.SinOut);
            Result = true;
        }
    }

    private void BackBtn_Clicked(object sender, EventArgs e)
    {
        TapBack();
    }

    private void CloseBtn_Clicked(object sender, EventArgs e)
    {
        TapClose();
    }

    private void YesBtn_Clicked(object sender, EventArgs e)
    {
        TapYes();
    }

    public void TapYes()
    {
        Result = true;
        IsOpen = false;
        ConfirmCommand?.Execute(IsMultiple ? SelectedItems : SelectedItem);
    }

    private void TapSelected(FilePickerOption item)
    {
        if (item.IsFolder)
        {
            Histories.Add(item.Name);
            LoadFile();
            return;
        }
        if (IsMultiple)
        {
            item.IsChecked = !item.IsChecked;
            return;
        }
        item.IsChecked = true;
        TapYes();
    }

    private void TapClose()
    {
        IsOpen = false;
        Result = true;
    }

    private void TapBack()
    {
        if (Histories.Count > 0)
        {
            Histories.RemoveAt(Histories.Count - 1);
            LoadFile();
        }
    }

    private void LoadFile()
    {
        if (Histories.Count < 1)
        {
            Title = "设备和驱动器";
            CanBackable = false;
            Items = LoadDrivers();
            return;
        }
        CanBackable = true;
        var path = Path.Combine(Histories.ToArray());
        Title = Histories.Last().Length > 20 ?
            Histories.Last()[..20] + "..." :
            path.Length > 20 ? string.Concat("...", path.AsSpan(path.Length - 20)) : path;
        _ = LoadFileAsync(path);
    }

    private async Task LoadFileAsync(string path)
    {
        var filter = IsFolderPicker || string.IsNullOrWhiteSpace(Filter) ? null : new Regex(Filter);
        var items = await GetFileAsync(path, IsFolderPicker,
            filter);
        MainThread.BeginInvokeOnMainThread(() => {
            Items = items;
        });
    }

    private IList<FilePickerOption> LoadDrivers()
    {
        var data = new List<FilePickerOption>();
        var items = Environment.GetLogicalDrives();
        foreach (var item in items)
        {
            data.Add(new FilePickerOption()
            {
                FileName = item,
                Name = item[..(item.Length - 1)],
                IsFolder = true,
            });
        }
        return data;
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