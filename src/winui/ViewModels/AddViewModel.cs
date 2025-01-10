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
    public class AddViewModel: BindableBase
    {
        public AddViewModel()
        {
            QueryCommand = new RelayCommand<string>(TapQuery);
        }

        private string _keywords = string.Empty;

        public string Keywords {
            get => _keywords;
            set => Set(ref _keywords, value);
        }


        private ObservableCollection<UserItemViewModel> _items = [];

        public ObservableCollection<UserItemViewModel> Items {
            get => _items;
            set => Set(ref _items, value);
        }

        public ICommand QueryCommand { get; private set; }

        private void TapQuery(string text)
        {
            Items.Add(new UserItemViewModel()
            {
                Name = text,
            });
        }
    }
}
