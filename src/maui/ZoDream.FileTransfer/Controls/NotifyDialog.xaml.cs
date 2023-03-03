using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Controls;

public partial class NotifyDialog : ContentView
{
	public NotifyDialog()
	{
		InitializeComponent();
        AgreeCommand = new AsyncRelayCommand<UserInfoOption>(TapAgree);
        DisagreeCommand = new AsyncRelayCommand<UserInfoOption>(TapDisagree);
        App.Repository.ChatHub.NewUser += Repository_NewUser;
    }


    public bool IsOpen {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(NotifyDialog),
            false, propertyChanged: (b, oldVal, newVal) => {
                (b as NotifyDialog)?.ToggleOpen((bool)oldVal, (bool)newVal);
            });


    public IList<IUser> Items {
        get { return (IList<IUser>)GetValue(ItemsProperty); }
        set { SetValue(ItemsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(IList<IUser>),
            typeof(NotifyDialog),
            new List<IUser>());

    private Grid? InnerPanel;
    public ICommand AgreeCommand { get; private set; }
    public ICommand DisagreeCommand { get; private set; }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        InnerPanel = GetTemplateChild("PART_InnerPanel") as Grid;
        var closeBtn = GetTemplateChild("PART_CloseBtn") as Button;
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
        }
    }


    private async Task TapAgree(UserInfoOption item)
    {
        var success = await App.Repository.ChatHub.AgreeAddUserAsync(item, false);
        item.Status = success ? 2 : 3;
    }

    private async Task TapDisagree(UserInfoOption item)
    {
        item.Status = 3;
        await App.Repository.ChatHub.AgreeAddUserAsync(item, false);
    }

    private void Repository_NewUser(IUser user)
    {
        foreach (var item in Items)
        {
            if (item.Id == user.Id)
            {
                return;
            }
        }
        var items = new List<IUser>
        {
            new UserInfoOption(user)
        };
        items.AddRange(Items);
        MainThread.BeginInvokeOnMainThread(() => {
            IsOpen = true;
            Items = items;
        });
    }

    private void CloseBtn_Clicked(object sender, EventArgs e)
    {
        IsOpen = false;
    }
}