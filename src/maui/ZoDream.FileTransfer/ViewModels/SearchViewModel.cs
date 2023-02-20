using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class SearchViewModel : ObservableObject
    {

        public SearchViewModel()
        {
            SearchCommand = new AsyncRelayCommand(TapSearch);
            AgreeCommand = new AsyncRelayCommand<UserInfoOption>(TapAgree);
            DisagreeCommand = new AsyncRelayCommand<UserInfoOption>(TapDisagree);
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

        private ObservableCollection<UserInfoOption> userItems = new();

        public ObservableCollection<UserInfoOption> UserItems
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

        private async Task TapAgree(UserInfoOption item)
        {
            var client = App.Repository.NetHub.Connect(item.Ip, item.Port);
            if (client == null)
            {
                item.Status = 3;
                return;
            }
            item.Status = 1;
            await client.SendAsync(new JSONMessage<UserInfoItem>()
            {
                Type = SocketMessageType.CallAddUser,
                Data = App.Repository.Option.FormatInfo()
            });
            var message = await client.ReceiveAsync(SocketMessageType.AddUser);
            if (message is BoolMessage o)
            {
                item.Status = o.Value ? 2 : 3;
                if (o.Value)
                {
                    App.Repository.Add(item);
                }
            }
            client.Dispose();
        }

        private async Task TapDisagree(UserInfoOption item)
        {
            item.Status = 3;
            //for (int i = UserItems.Count - 1; i >= 0; i--)
            //{
            //    if (item.Ip == UserItems[i].Ip)
            //    {
            //        UserItems.RemoveAt(i);
            //    }
            //}
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
            var user = App.Repository.Get(ip, port);
            if (user != null)
            {
                UserItems.Add(new UserInfoOption()
                {
                    Id = user.Id,
                    Name = user.Name,
                    Ip = ip,
                    Port = port,
                    Avatar = user.Avatar,
                    Status = 2
                });
                return;
            }
            var client = App.Repository.NetHub.Connect(ip, port);
            if (client == null)
            {
                return;
            }
            client.Send(SocketMessageType.CallInfo);
            var message = await client.ReceiveAsync(SocketMessageType.Info);
            if (message is JSONMessage<UserInfoItem> o)
            {
                UserItems.Add(new UserInfoOption(o.Data));
            }
            client.Dispose();
        }
    }
}
