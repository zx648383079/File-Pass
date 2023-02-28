using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Views;

public partial class ChatPage : ContentPage
{
	public ChatPage()
	{
		InitializeComponent();
        Loaded += ChatPage_Loaded;
	}

    private void ChatPage_Loaded(object sender, EventArgs e)
    {
        var vm = BindingContext as ChatViewModel;
        vm.StoragePicker = FilePicker;
        vm.UserPicker = UserPicker;
    }
}