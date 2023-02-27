namespace ZoDream.FileTransfer.Controls;

public partial class MessageTipListItem : ContentView
{
	public MessageTipListItem()
	{
		InitializeComponent();
	}



	public string Text {
		get { return (string)GetValue(TextProperty); }
		set { SetValue(TextProperty, value); }
	}

	// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), 
			typeof(MessageTipListItem), string.Empty);


}