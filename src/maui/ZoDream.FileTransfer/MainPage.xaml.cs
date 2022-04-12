using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            BindingContext = ViewModel;
            IpTb.Text = $"{Ip.Get()}:65530";
            Mode = Width >= 800 ? PanelMode.All : PanelMode.Receive;
        }

        public MainViewModel ViewModel = new();
        private TransferClient client;
        private TransferServer server;

        private PanelMode mode;

        public PanelMode Mode
        {
            get { return mode; }
            set {
                mode = value;
                switch (value)
                {
                    case PanelMode.Receive:
                        ReceivePanel.IsVisible = true;
                        SendPanel.IsVisible = false;
                        break;
                    case PanelMode.Send:
                        ReceivePanel.IsVisible = false;
                        SendPanel.IsVisible = true;
                        break;
                    case PanelMode.All:
                        ReceivePanel.IsVisible = true;
                        SendPanel.IsVisible = true;
                        break;
                    default:
                        break;
                }
            }
        }


        private void ListenBtn_Clicked(object sender, EventArgs e)
        {
            var ip = Ip.FormatIp(IpTb.Text);
            
            ViewModel.ServerMessage = "接收中...";
            var saveFolder = "";
            ListenBtn.IsEnabled = false;
            if (server == null)
            {
                server = new TransferServer();
            }
            server.Open(ip.Item1, ip.Item2);
            server.Listen(saveFolder, (FileInfoItem item) =>
            {
                App.Current.Dispatcher.Dispatch(() =>
                {
                    ViewModel.AddFile(item, false);
                });
            }, (current, total, file) =>
            {
                if (string.IsNullOrEmpty(file))
                {
                    return;
                }
                App.Current.Dispatcher.Dispatch(() =>
                {
                    ViewModel.UpdateFile(file, current, total, false);
                });
            });
        }

        private void PickerBtn_Clicked(object sender, EventArgs e)
        {
            PickFile();
        }

        private async void PickFile()
        {
            var items = await FilePicker.PickMultipleAsync();
            if (items.Count() < 1)
            {
                return;
            }
            PreviewSend();
            client.SendFiles(items.Select(i => i.FullPath), SendFileInit, SendFileProgress);
        }

        private bool PreviewSend()
        {
            var ip = Ip.FormatIp(DistIpTb.Text);
            if (client == null)
            {
                client = new TransferClient();
            }
            client.Open(ip.Item1, ip.Item2);
            ViewModel.ClientMessage = "准备发送...";
            return true;
        }


        private void SendFileInit(FileInfoItem item)
        {
            App.Current.Dispatcher.Dispatch(() =>
            {
                PickerBtn.IsEnabled = false;
                ViewModel.AddFile(item, true);
            });
        }

        private void SendFileProgress(long current, long total, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return;
            }
            App.Current.Dispatcher.Dispatch(() =>
            {
                if (current == total)
                {
                    PickerBtn.IsEnabled = true;
                }
                ViewModel.UpdateFile(file, current, total, true);
            });
        }

        public enum PanelMode
        {
            Receive,
            Send,
            All
        }
    }
}