namespace ZoDream.FileTransfer.Controls;

public partial class UserSelectDialog : ContentView
{
	public UserSelectDialog()
	{
		InitializeComponent();
	}



	public bool IsOpen
	{
		get { return (bool)GetValue(IsOpenProperty); }
		set { SetValue(IsOpenProperty, value); }
	}

	public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(UserSelectDialog), 
			false, propertyChanged: (b, oldVal, newVal) =>
			{
				(b as UserSelectDialog)?.ToggleOpen((bool)oldVal, (bool)newVal);
			});

    private void ToggleOpen(bool oldVal, bool newVal)
    {
		if (newVal)
		{
            DialogBox.TranslateTo(0, 0, 500, Easing.SinIn);
        } else
		{
            DialogBox.TranslateTo(0, Height, 500, Easing.SinOut);
        }
    }

    private void CloseBtn_Clicked(object sender, EventArgs e)
    {
		IsOpen = false;
    }
}