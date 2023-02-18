using ZoDream.FileTransfer.Views;

namespace ZoDream.FileTransfer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("Home", typeof(HomePage));
            Routing.RegisterRoute("Chat", typeof(ChatPage));
            Routing.RegisterRoute("Profile", typeof(ProfilePage));
            Routing.RegisterRoute("Search", typeof(SearchPage));
            Routing.RegisterRoute("Setting", typeof(SettingPage));

        }
    }
}