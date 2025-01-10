using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using ZoDream.FileTransfer.Pages;

namespace ZoDream.FileTransfer.ViewModels
{
    internal partial class AppViewModel
    {
        private Frame _rootFrame;

        private Frame _sideFrame;
        private Frame _bodyFrame;

        public void Binding(Frame side, Frame body)
        {
            _sideFrame = side;
            _bodyFrame = body;
            Navigate("home");
        }

        public void Navigate(string pageName)
        {
            Navigate(pageName, 0);
        }

        public void Navigate(string pageName, int parameter)
        {
            switch (pageName)
            {
                case "profile":
                    _bodyFrame.Navigate(typeof(ProfilePage));
                    break;
                case "group":
                    if (parameter > 0)
                    {
                        _sideFrame.Navigate(typeof(GroupUserPage));
                        _bodyFrame.Navigate(typeof(ChatRoomPage));
                        break;
                    }
                    _sideFrame.Navigate(typeof(GroupPage));
                    break;
                case "setting":
                    _bodyFrame.Navigate(typeof(SettingPage));
                    break;
                case "add":
                    _bodyFrame.Navigate(typeof(AddPage));
                    break;
                default:
                    if (parameter > 0)
                    {
                        _bodyFrame.Navigate(typeof(ChatRoomPage));
                    }
                    if (_sideFrame.CurrentSourcePageType != typeof(FriendsPage))
                    {
                        _sideFrame.Navigate(typeof(FriendsPage));
                    }
                    break;
            }
        }

        public void Navigate<T>() where T : Page
        {
            _rootFrame.Navigate(typeof(T));
            BackEnabled = typeof(T) != typeof(StartupPage);
        }

        public void Navigate<T>(object parameter) where T : Page
        {
            _rootFrame.Navigate(typeof(T), parameter);
            BackEnabled = typeof(T) != typeof(StartupPage);
        }

        public void NavigateBack()
        {
            _rootFrame.GoBack();
            BackEnabled = false;
        }
        /// <summary>
        ///  起始页
        /// </summary>
        private void Startup()
        {
            var app = AppInstance.GetCurrent();
            var args = app.GetActivatedEventArgs();
            if (args.Kind != ExtendedActivationKind.File)
            {
                Navigate<StartupPage>();
                return;
            }
            if (args.Data is FileActivatedEventArgs e)
            {
                Navigate<WorkspacePage>(e.Files);
                //Task.Factory.StartNew(() => 
                //{
                //    Thread.Sleep(1000);
                //    DispatcherQueue.TryEnqueue(() => {
                //        _ = ConfirmAsync();
                //    });
                //});
                return;
            }
            
        }
    }
}
