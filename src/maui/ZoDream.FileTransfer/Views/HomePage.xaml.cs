using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Views;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
        BindingContext = ViewModel;
    }

	internal HomeViewModel ViewModel = new();
}