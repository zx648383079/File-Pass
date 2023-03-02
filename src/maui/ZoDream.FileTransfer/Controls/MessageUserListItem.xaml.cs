using System.Windows.Input;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Controls;

public partial class MessageUserListItem : ContentView
{
	public MessageUserListItem()
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
            typeof(MessageUserListItem), null);




    public UserMessageItem ItemSource {
        get { return (UserMessageItem)GetValue(ItemSourceProperty); }
        set { SetValue(ItemSourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ItemSource.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ItemSourceProperty =
        BindableProperty.Create(nameof(ItemSource), typeof(UserMessageItem),
            typeof(MessageUserListItem), null,
            propertyChanged: (d, _, o) => {
                (d as MessageUserListItem)?.UpdateView();
            });


    public ICommand Command {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for YesCommand.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand),
            typeof(MessageUserListItem), null);

    private void UpdateView()
    {
        if (ItemSource == null)
        {
            return;
        }
        HorizontalOptions = ItemSource.IsSender ? LayoutOptions.End : LayoutOptions.Start;
        CornerRadius = new CornerRadius(ItemSource.IsSender ? MessageListView.MessageRadius : 0,
                    ItemSource.IsSender ? 0 : MessageListView.MessageRadius,
                    MessageListView.MessageRadius, MessageListView.MessageRadius);
    }
}