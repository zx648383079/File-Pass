using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
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
            App.Repository.ChatHub.NewUser += ChatHub_NewUser;
        }

        private void ChatHub_NewUser(IUser user)
        {
            UserItems.Add(new UserInfoOption(user));
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
            item.Status = 1;
            var success = await App.Repository.ChatHub.AddUserAsync(item);
            item.Status = success ? 2 : 3;
        }

        private Task TapDisagree(UserInfoOption item)
        {
            item.Status = 3;
            return Task.CompletedTask;
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
            await App.Repository.ChatHub.SearchUsersAsync(Ip, Port);
            IsLoading = false;
        }
    }
}
