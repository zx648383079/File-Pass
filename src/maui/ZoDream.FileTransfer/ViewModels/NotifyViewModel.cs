using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ZoDream.FileTransfer.Controls;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class NotifyViewModel: ObservableObject
    {
        public NotifyViewModel()
        {
            AgreeCommand = new AsyncRelayCommand<UserInfoOption>(TapAgree);
            DisagreeCommand = new AsyncRelayCommand<UserInfoOption>(TapDisagree);
            App.Repository.NewUser += Repository_NewUser;
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

        public ICommand AgreeCommand { get; private set; }
        public ICommand DisagreeCommand { get; private set; }
        public UserSelectDialog Context { get; internal set; }

        private async Task TapAgree(UserInfoOption item)
        {
            
            var client = App.Repository.NetHub.Connect(item.Ip, item.Port);
            if (client == null)
            {
                item.Status = 3;
                return;
            }
            await client.SendAsync(new BoolMessage()
            {
                Type = SocketMessageType.AddUser,
                Value = true
            });
            item.Status = 2;
            App.Repository.Add(item);
        }

        private async Task TapDisagree(UserInfoOption item)
        {
            item.Status = 3;
            var client = App.Repository.NetHub.Connect(item.Ip, item.Port);
            if (client == null)
            {
                return;
            }
            await client.SendAsync(new BoolMessage() { 
                Type = SocketMessageType.AddUser,
                Value = false
            });
            //for (int i = UserItems.Count - 1; i >= 0; i--)
            //{
            //    if (item.Ip == UserItems[i].Ip)
            //    {
            //        UserItems.RemoveAt(i);
            //    }
            //}
        }

        private void Repository_NewUser(UserInfoItem user)
        {
            foreach (var item in UserItems)
            {
                if (item.Id == user.Id)
                {
                    return;
                }
            }
            Context.IsOpen = true;
            UserItems.Prepend(new UserInfoOption(user));
        }
    }
}
