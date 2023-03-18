using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Controls;

public partial class MessageFolderListItem : ContentView
{
	public MessageFolderListItem()
	{
		InitializeComponent();
	}


    public CornerRadius CornerRadius {
        get { return (CornerRadius)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius),
            typeof(MessageFolderListItem), null);




    public FolderMessageItem ItemSource {
        get { return (FolderMessageItem)GetValue(ItemSourceProperty); }
        set { SetValue(ItemSourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ItemSource.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ItemSourceProperty =
        BindableProperty.Create(nameof(ItemSource), typeof(FolderMessageItem),
            typeof(MessageFolderListItem), null,
            propertyChanged: (d, _, o) => {
                (d as MessageFolderListItem)?.UpdateView();
            });


    public ICommand TapCommand {
        get { return (ICommand)GetValue(TapCommandProperty); }
        set { SetValue(TapCommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for YesCommand.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand),
            typeof(MessageFolderListItem), null);


    private BoxView? Hr;
    private ProgressBar? SpeedBar;
    private Label? StatusTb;
    private Button? RevBtn;
    private Button? CancelBtn;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Hr = GetTemplateChild("PART_Hr") as BoxView;
        SpeedBar = GetTemplateChild("PART_ProgressBar") as ProgressBar;
        StatusTb = GetTemplateChild("PART_StatusTb") as Label;
        RevBtn = GetTemplateChild("PART_RevBtn") as Button;
        CancelBtn = GetTemplateChild("PART_Cancel") as Button;
        if (RevBtn != null)
        {
            RevBtn.Clicked += RevBtn_Clicked;
        }
        if (CancelBtn != null)
        {
            CancelBtn.Clicked += CancelBtn_Clicked;
        }
    }

    private void CancelBtn_Clicked(object? sender, EventArgs e)
    {
        TapCommand?.Execute(new MessageTapEventArg(ItemSource, MessageTapEvent.Cancel));
    }

    private void RevBtn_Clicked(object? sender, EventArgs e)
    {
        TapCommand?.Execute(new MessageTapEventArg(ItemSource, MessageTapEvent.Confirm));
    }

    private void UpdateView()
    {
        if (ItemSource == null)
        {
            return;
        }
        HorizontalOptions = ItemSource.IsSender ? LayoutOptions.End : LayoutOptions.Start;
        CornerRadius = new CornerRadius(ItemSource.IsSender ? MessageListView.MessageRadius : 0,
                    ItemSource.IsSender ? 0 : MessageListView.MessageRadius,
                    0, 0);
        ItemSource.PropertyChanged -= ItemSource_PropertyChanged;
        ItemSource.PropertyChanged += ItemSource_PropertyChanged;
        ChangeStatus();
    }

    private void ItemSource_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ItemSource.Status):
                MainThread.BeginInvokeOnMainThread(() => {
                    ChangeStatus();
                });
                break;
            case nameof(ItemSource.Progress):
                if (ItemSource.Status == FileMessageStatus.Transferring)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        SpeedBar!.Progress = ItemSource.Progress / ItemSource.Size;
                        StatusTb!.Text = $"{ItemSource.FileName}\n{Disk.FormatSize(ItemSource.Speed)}/s {Disk.FormatSize(ItemSource.Progress)}/{Disk.FormatSize(ItemSource.Size)}";
                    });
                }
                break;
            default:
                break;
        }
    }

    private void ChangeStatus()
    {
        var status = ItemSource.Status;
        Hr!.IsVisible = status != FileMessageStatus.Transferring;
        SpeedBar!.IsVisible = status == FileMessageStatus.Transferring;
        StatusTb!.IsVisible = status != FileMessageStatus.None;
        RevBtn!.IsVisible = status == FileMessageStatus.None;
        CancelBtn!.IsVisible = status == FileMessageStatus.None ||
            status == FileMessageStatus.Transferring;
        switch (ItemSource.Status)
        {
            case FileMessageStatus.Transferring:
                StatusTb.Text = "";
                SpeedBar.Progress = 0;
                break;
            case FileMessageStatus.Success:
                StatusTb.Text = "已完成";
                break;
            case FileMessageStatus.Failure:
                StatusTb.Text = "已失败";
                break;
            case FileMessageStatus.Canceled:
                StatusTb.Text = "已取消";
                break;
            default:
                break;
        }
    }
}