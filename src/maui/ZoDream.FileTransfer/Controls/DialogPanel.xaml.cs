namespace ZoDream.FileTransfer.Controls;

public partial class DialogPanel : ContentView
{
	public DialogPanel()
	{
		InitializeComponent();
	}

    private Grid InnerPanel;

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
            });

    private void ToggleOpen(bool oldVal, bool newVal)
    {

        if (newVal)
        {
            InnerPanel?.TranslateTo(0, 0, 500, Easing.SinIn);
        }
        else
        {
            InnerPanel?.TranslateTo(0, 500, 500, Easing.SinOut);
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InnerPanel = GetTemplateChild("PART_InnerPanel") as Grid;
        var btn = GetTemplateChild("PART_CloseBtn") as Button;
        if (btn != null)
        {
            btn.Clicked += (s, e) => {
                IsOpen = false;
            };
        }
    }
}