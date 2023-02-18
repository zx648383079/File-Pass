using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class HomeViewModel: ObservableObject
    {

        public HomeViewModel()
        {
            ChatCommand = new AsyncRelayCommand<UserItem>(GoToChat);
            SearchCommand = new AsyncRelayCommand(GoToSearch);
            SettingCommand = new AsyncRelayCommand(GoToSetting);
            _ = LoadAsync();
        }

        

        private ObservableCollection<UserItem> userItems = new();

        public ObservableCollection<UserItem> UserItems
        {
            get => userItems;
            set {
                userItems = value;
                OnPropertyChanged();
            }
        }

        public ICommand ChatCommand { get; private set; }

        public ICommand SearchCommand { get; private set; }
        public ICommand SettingCommand { get; private set; }

        private async Task GoToChat(UserItem user)
        {
            await Shell.Current.GoToAsync($"Chat?user={user.Id}");
        }

        private async Task GoToSearch()
        {
            await Shell.Current.GoToAsync("Search");
        }

        private async Task GoToSetting()
        {
            await Shell.Current.GoToAsync("Setting");
        }

        private async Task LoadAsync()
        {
            await App.Repository.WaitBoot();
            foreach (var item in App.Repository.UserItems)
            {
                UserItems.Add(item);
            }
        }
    }
}
