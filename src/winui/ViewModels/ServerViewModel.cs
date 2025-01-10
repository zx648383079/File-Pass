using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ServerViewModel: BindableBase
    {



        private ObservableCollection<ClientItemViewModel> _items = [];

        public ObservableCollection<ClientItemViewModel> Items {
            get => _items;
            set => Set(ref _items, value);
        }

        private string _message = string.Empty;

        public string Message {
            get => _message;
            set => Set(ref _message, value);
        }

        public string StatusInfo => string.Format("在线：{0} | 离线：{1} ",
            Items.Where(i => i.IsOnline).Count(), Items.Where(i => !i.IsOnline).Count());

    }
}
