namespace ZoDream.FileTransfer.Controls;

public partial class DialogPanel : ContentView
{
	public DialogPanel()
	{
		InitializeComponent();
	}



    public string Title {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(DialogPanel), string.Empty);



    public bool IsOpen {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(DialogPanel),
            true, propertyChanged: (b, oldVal, newVal) => {
                (b as DialogPanel)?.ToggleOpen((bool)oldVal, (bool)newVal);
            },
            defaultValueCreator: b => {
                (b as DialogPanel)?.ToggleOpen(true, false);
                return true;
            });

    private void ToggleOpen(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            DialogBox.TranslateTo(0, 0, 500, Easing.SinIn);
        }
        else
        {
            DialogBox.TranslateTo(0, Height, 500, Easing.SinOut);
        }
    }

    private void CloseBtn_Clicked(object sender, EventArgs e)
    {
        IsOpen = false;
    }
}