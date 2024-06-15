using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Controls;

public class MessageListView : ContentView
{
    internal const int MessageRadius = 10;


    public MessageListView()
	{
        InnerPanel = new VerticalStackLayout()
        {
            BackgroundColor = Colors.Gray,
            Padding = new Thickness(20)
        };
		Content = ScrollBar = new ScrollView()
        {
            Content = InnerPanel
        };
	}

    private readonly VerticalStackLayout InnerPanel;
    private readonly ScrollView ScrollBar;

    public int MaxTime
    {
        get { return (int)GetValue(MaxTimeProperty); }
        set { SetValue(MaxTimeProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MaxTime.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty MaxTimeProperty =
        BindableProperty.Create(nameof(MaxTime), typeof(int), typeof(MessageListView), 600);


    internal IEnumerable<MessageItem> ItemsSource
	{
		get { return (IEnumerable<MessageItem>)GetValue(ItemsSourceProperty); }
		set { SetValue(ItemsSourceProperty, value); }
	}

	// Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<MessageItem>), typeof(MessageListView), null, BindingMode.OneWay, null, (d, newVal, oldVal) =>
		{
			(d as MessageListView)?.OnItemsSourceChanged();
		});


    public ICommand TapCommand
    {
        get { return (ICommand)GetValue(TapCommandProperty); }
        set { SetValue(TapCommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for YesCommand.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand),
            typeof(MessageListView), null);

    private void OnItemsSourceChanged()
	{
        if (ItemsSource == null)
        {
            return;
        }
        BindListener();
        MainThread.BeginInvokeOnMainThread(() => {
            RefreshView();
        });
	}

    private void RefreshView()
    {
        InnerPanel.Children.Clear();
        DateTime lastTime = DateTime.MinValue;
        var now = DateTime.Now;
        // var exist = new List<int>();
        var beforeMessage = false;
        foreach (var item in ItemsSource)
        {
            //if (exist.IndexOf(item.Id) >= 0)
            //{
            //    continue;
            //}
            var time = item.CreatedAt;
            if (lastTime == DateTime.MinValue || Time.SecondDiffer(time, lastTime) > MaxTime)
            {
                lastTime = time;
                InnerPanel.Children.Add(CreateTip(Time.FormatAgo(time, now)));
                beforeMessage = false;
            }
            // exist.Add(item.Id);
            var ele = CreateMessage(item);
            if (beforeMessage)
            {
                ele.Margin = new Thickness(0, 10, 0, 0);
            }
            InnerPanel.Children.Add(ele);
            beforeMessage = true;
        }
        ScrollBar.ScrollToAsync(0, InnerPanel.Height, false);
    }

    private View CreateTip(string value)
    {
        return new MessageTipListItem()
        {
            Text = value,
        };
    }

    private View CreateMessage(MessageItem item)
    {
        if (item is ActionMessageItem a)
        {
            return CreateTip(a.Content);
        }
        if (item is TextMessageItem text)
        {
            return CreateTextMessage(text);
        }
        else if (item is UserMessageItem user)
        {
            return new MessageUserListItem()
            {
                ItemSource = user,
                Command = new Command(() => {
                    TapCommand?.Execute(new MessageTapEventArg(user, MessageTapEvent.Confirm));
                })
            };
        }
        else if (item is SyncMessageItem sync)
        {
            return new MessageSyncListItem()
            {
                ItemSource = sync,
                TapCommand = TapCommand
            };
        }
        else if (item is FolderMessageItem folder)
        {
            return new MessageFolderListItem()
            {
                ItemSource = folder,
                TapCommand = TapCommand
            };
        }
        else if (item is FileMessageItem file)
        {
            return CreateFileMessage(file);
        }
        return new Grid();
    }

    private View CreateTextMessage(TextMessageItem item)
    {
        return new MessageTextListItem()
        {
            ItemSource = item,
        };
    }

    private View CreateFileMessage(FileMessageItem item)
    {
        return new MessageFileListItem() { 
            ItemSource = item,
            TapCommand = TapCommand
        };
    }


    private void BindListener()
    {
        if (ItemsSource == null)
        {
            return;
        }
        if (ItemsSource is INotifyCollectionChanged obj)
        {
            obj.CollectionChanged -= Obj_CollectionChanged;
            obj.CollectionChanged += Obj_CollectionChanged;
        }
        if (ItemsSource is INotifyPropertyChanged o)
        {
            o.PropertyChanged -= Obj_PropertyChanged;
            o.PropertyChanged += Obj_PropertyChanged;
        }
    }

    private void Obj_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => {
            RefreshView();
        });
    }

    private void Obj_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => {
            RefreshView();
        });
    }

}
