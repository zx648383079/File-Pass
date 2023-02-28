using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Controls;

public partial class UserPicker : ContentView
{
	public UserPicker()
	{
		InitializeComponent();
	}


    public bool IsOpen {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(UserPicker),
            false, propertyChanged: (b, oldVal, newVal) => {
                (b as UserPicker)?.ToggleOpen((bool)oldVal, (bool)newVal);
            });


    public IList<UserInfoOption> Items {
        get { return (IList<UserInfoOption>)GetValue(ItemsProperty); }
        set { SetValue(ItemsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<UserInfoOption>), 
            typeof(UserPicker),
            new List<UserInfoOption>());

    private bool Result = false;
    private Grid InnerPanel;

    public UserInfoOption SelectedItem {
        get {
            if (Items is null)
            {
                return null;
            }
            foreach (var item in Items)
            {
                if (!item.IsChecked)
                {
                    continue;
                }
                return item;
            }
            return null;
        }
    }

    public IList<UserInfoOption> SelectedItems {
        get {
            var items = new List<UserInfoOption>();
            if (Items is null)
            {
                return items;
            }
            foreach (var item in Items)
            {
                if (!item.IsChecked)
                {
                    continue;
                }
                items.Add(item);
            }
            return items;
        }
    }

    public Task<bool> ShowAsync()
    {
        Result = false;
        IsOpen = true;
        return Task.Factory.StartNew(() => {
            while (!Result)
            {

            }
            return SelectedItem is not null;
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

    private void CloseBtn_Clicked(object sender, EventArgs e)
    {
        TapClose();
    }

    private void YesBtn_Clicked(object sender, EventArgs e)
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
        Result = true;
    }
}