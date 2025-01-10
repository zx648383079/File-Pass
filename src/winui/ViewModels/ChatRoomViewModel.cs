using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.Model;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ChatRoomViewModel: BindableBase
    {
        public ChatRoomViewModel()
        {
            var user = new User(2, "zodream", "");
            var sender = new User(1, "sender", "ms-appx:///Assets/Logo.png");
            Items.AddText(user, "哈哈");
            Items.AddText(user, "哈哈12");
            Items.AddText(sender, "哈哈123123");
        }

        private string _content = string.Empty;

        public string Content {
            get => _content;
            set => Set(ref _content, value);
        }


        private MessageCollection _items = new(1);

        public MessageCollection Items {
            get => _items;
            set => Set(ref _items, value);
        }


    }
}
