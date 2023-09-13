using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Views;

public partial class SettingPage : ContentPage
{
	public SettingPage()
	{
		InitializeComponent();
	}

	private SettingViewModel ViewModel => (SettingViewModel)BindingContext;

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
		ViewModel.AutoSave();
    }
}