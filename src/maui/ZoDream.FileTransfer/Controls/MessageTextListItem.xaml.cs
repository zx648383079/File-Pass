using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Controls;

public partial class MessageTextListItem : ContentView
{
	public MessageTextListItem()
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
			typeof(MessageTextListItem), null);




	public TextMessageItem ItemSource {
		get { return (TextMessageItem)GetValue(ItemSourceProperty); }
		set { SetValue(ItemSourceProperty, value); }
	}

	// Using a DependencyProperty as the backing store for ItemSource.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty ItemSourceProperty =
        BindableProperty.Create(nameof(ItemSource), typeof(TextMessageItem), 
			typeof(MessageTextListItem), null, 
			propertyChanged: (d, _, o) => {
				(d as MessageTextListItem)?.UpdateView();
			});


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