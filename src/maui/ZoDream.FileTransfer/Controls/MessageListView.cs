using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Controls;

public class MessageListView : ContentView
{
	public MessageListView()
	{
        InnerPanel = new VerticalStackLayout()
        {
            BackgroundColor = Colors.Gray,
            Padding = new Thickness(20)
        };
		Content = new ScrollView()
        {
            Content = InnerPanel
        };
	}

    private VerticalStackLayout InnerPanel;

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
			(d as MessageListView).OnItemsSourceChanged();
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
        RefreshView();
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
    }

    private View CreateTip(string value)
    {
        return new Label()
        {
            Text = value,
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(0, 10)
        };
    }

    private View CreateMessage(MessageItem item)
    {
        if (item is ActionMessageItem a)
        {
            return CreateTip(a.Content);
        }
        var radius = 10;
        var bottomRadius = item is TextMessageItem ? radius : 0;
        var box = new Grid()
        {
            HorizontalOptions = item.IsSender ? LayoutOptions.End : LayoutOptions.Start,
            Children =
            {
                new BoxView()
                {
                    Color = Colors.White,
                    CornerRadius = new CornerRadius(item.IsSender ? radius : 0,
                    item.IsSender ? 0 : radius, bottomRadius, bottomRadius)
                },
            }
        };
        if (item is TextMessageItem text)
        {
            box.Children.Add(CreateTextMessage(text));
        } else if (item is FileMessageItem file)
        {
            box.Children.Add(CreateFileMessage(file));
        }
        return box;
    }

    private View CreateTextMessage(TextMessageItem item)
    {
        return new Label()
        {
            Padding = new Thickness(12, 5),
            Text = item.Content
        };
    }

    private View CreateFileMessage(FileMessageItem item)
    {
        var size = new Label()
        {
            TextColor = Colors.Gray,
            Text = Disk.FormatSize(item.Size)
        };
        if (item.Status == FileMessageStatus.Transferring)
        {
            size.Text = $"{Disk.FormatSize(item.Speed)}/s {Disk.FormatSize(item.Progress)}/{Disk.FormatSize(item.Size)}";
        }
        Grid.SetColumn(size, 1);
        var box = new VerticalStackLayout()
        {
            WidthRequest = 300,
            Children =
            {
                new Grid()
                {
                    Padding = new Thickness(12, 6),
                    ColumnDefinitions = new ColumnDefinitionCollection(
                        new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)),
                    Children =
                    {
                        new Label()
                        {
                            HorizontalOptions = LayoutOptions.Center,
                            Text = item.FileName
                        },
                        size,
                    }
                },
                new BoxView()
                {
                    Color = Colors.Gray,
                    HeightRequest = 2,
                    HorizontalOptions= LayoutOptions.Fill,
                },
            }
        };
        var actionBox = new Grid()
        {
            Children =
            {
                new Button()
                {
                    Text = "接收"
                },
            }
        };
        box.Children.Add(actionBox);
        if (item.Status == FileMessageStatus.None)
        {
            var cancelBtn = new Button()
            {
                Text = "取消"
            };
            actionBox.Children.Add(cancelBtn);
            Grid.SetColumn(cancelBtn, 1);
        }
        if (actionBox.Children.Count > 1)
        {
            actionBox.ColumnDefinitions = new ColumnDefinitionCollection(
                actionBox.Children.Select(i => new ColumnDefinition(GridLength.Star)).ToArray()
                );
            for ( var i = 0; i < actionBox.Children.Count; i++ )
            {
                var btn = actionBox.Children[i] as Button;
                btn.CornerRadius = 0;
                Grid.SetColumn(btn, i);
            }
        }
        return box;
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

    private void Obj_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        RefreshView();
    }

    private void Obj_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshView();
    }

}
