using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Controls;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class NotifyViewModel: ObservableObject
    {
        public NotifyViewModel()
        {
            AgreeCommand = new AsyncRelayCommand<UserInfoOption>(TapAgree);
            DisagreeCommand = new AsyncRelayCommand<UserInfoOption>(TapDisagree);
            App.Repository.ChatHub.NewUser += Repository_NewUser;
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
            var success = await App.Repository.ChatHub.AgreeAddUserAsync(item, false);
            item.Status = success ? 2 : 3;
        }

        private async Task TapDisagree(UserInfoOption item)
        {
            item.Status = 3;
            await App.Repository.ChatHub.AgreeAddUserAsync(item, false);
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
