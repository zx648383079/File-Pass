using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Repositories;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class SearchViewModel : ObservableObject
    {

        public SearchViewModel()
        {
            SearchCommand = new AsyncRelayCommand(TapSearch);
            AgreeCommand = new AsyncRelayCommand<UserItem>(TapAgree);
            DisagreeCommand = new AsyncRelayCommand<UserItem>(TapDisagree);
        }

        private bool isLoading = false;

        public bool IsLoading
        {
            get { return isLoading; }
            set { 
                isLoading = value;
                OnPropertyChanged();
            }
        }


        private string ip;

        public string Ip
        {
            get { return ip; }
            set
            {
                ip = value;
                OnPropertyChanged();
            }
        }


        private int port = Constants.DEFAULT_PORT;

        public int Port
        {
            get { return port; }
            set
            {
                port = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<UserItem> userItems = new();

        public ObservableCollection<UserItem> UserItems
        {
            get => userItems;
            set
            {
                userItems = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand SearchCommand { get; private set; }
        public ICommand AgreeCommand { get; private set; }
        public ICommand DisagreeCommand { get; private set; }

        private async Task TapAgree(UserItem item)
        {
            App.Repository.Add(item);
        }

        private async Task TapDisagree(UserItem item)
        {
            for (int i = UserItems.Count - 1; i >= 0; i--)
            {
                if (item.Ip == UserItems[i].Ip)
                {
                    UserItems.RemoveAt(i);
                }
            }
        }

        private async Task TapSearch()
        {
            if (isLoading)
            {
                return;
            }
            UserItems.Clear();
            IsLoading = true;
            if (!string.IsNullOrWhiteSpace(Ip))
            {
                await ConnectAsync(Ip, Port);
                IsLoading = false;
                return;
            }
            foreach (var item in await Utils.Ip.GetGroupOtherIpAsync())
            {
                await ConnectAsync(item, Port);
            }
            IsLoading = false;
        }

        private async Task ConnectAsync(string ip, int port)
        {
            var client = App.Repository.NetHub.Connect(ip, port);
            if (client == null)
            {
                return;
            }
            client.Send(Network.SocketMessageType.CallInfo);
            while (true)
            {
                var message = await client.ReceiveAsync();
                
            }
        }
    }
}
