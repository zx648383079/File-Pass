using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Controls;

public partial class PermissionDialog : ContentView
{
	public PermissionDialog()
	{
		InitializeComponent();
        Loaded += PermissionDialog_Loaded;
	}

    private void PermissionDialog_Loaded(object? sender, EventArgs e)
    {
        _ = CheckPermissionAsync();
    }

    private async Task CheckPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();
        if (status != PermissionStatus.Granted)
        {
            if (await ShowAsync())
            {
                await Permissions.RequestAsync<Permissions.NetworkState>();
            }
            return;
        }
        status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (status != PermissionStatus.Granted)
        {
            if (await ShowAsync())
            {
                await Permissions.RequestAsync<Permissions.StorageWrite>();
            }
            return;
        }
        status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status != PermissionStatus.Granted)
        {
            if (await ShowAsync())
            {
                await Permissions.RequestAsync<Permissions.StorageRead>();
            }
            return;
        }
    }

    public bool IsOpen {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(PermissionDialog),
            false, propertyChanged: (b, oldVal, newVal) => {
                (b as PermissionDialog)?.ToggleOpen((bool)oldVal, (bool)newVal);
            });


    public IList<PermissionItem> Items {
        get { return (IList<PermissionItem>)GetValue(ItemsProperty); }
        set { SetValue(ItemsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<PermissionItem>),
            typeof(PermissionDialog),
            PermissionItem.PermissionItems);

    private bool? Result = false;
    private Grid? InnerPanel;

    public Task<bool> ShowAsync()
    {
        Result = null;
        IsOpen = true;
        return Task.Factory.StartNew(() => {
            while (Result is null)
            {

            }
            return Result == true;
        });
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InnerPanel = GetTemplateChild("PART_InnerPanel") as Grid;
        var yesBtn = GetTemplateChild("PART_YesBtn") as Button;
        var closeBtn = GetTemplateChild("PART_CloseBtn") as Button;
        if (yesBtn != null)
        {
            yesBtn.Clicked += YesBtn_Clicked;
        }
        if (closeBtn != null)
        {
            closeBtn.Clicked += CloseBtn_Clicked;
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

    private void CloseBtn_Clicked(object? sender, EventArgs e)
    {
        TapClose();
    }

    private void YesBtn_Clicked(object? sender, EventArgs e)
    {
        TapYes();
    }

    public void TapYes()
    {
        Result = true;
        IsOpen = false;
    }

    private void TapClose()
    {
        IsOpen = false;
        Result = false;
    }
}