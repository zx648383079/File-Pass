using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class FriendViewModel: BindableBase
    {

        public FriendViewModel()
        {
            TapItemCommand = new RelayCommand<ContactItemViewModel>(TapItem);
            ItemsSource.IsSourceGrouped = true;
            ItemsSource.Source = Items;
            Items.Add(new("A")
            {
                {new()
                {
                    Id = 1,
                    Name = "Aaa",
                    Avatar = "ms-appx:///Assets/Logo.png",
                    LastAt = DateTime.Now,
                    LastMessage = "huajjjj "
                } }
            });
        }

        public CollectionViewSource ItemsSource { get; private set; } = new();

        private ObservableCollection<ContactGroupViewModel> _items = [];

        public ObservableCollection<ContactGroupViewModel> Items {
            get => _items;
            set => Set(ref _items, value);
        }


        public ICommand TapItemCommand { get; private set; }

        private void TapItem(ContactItemViewModel item)
        {
            App.ViewModel.Navigate("home", item.Id);
        }
    }
}
