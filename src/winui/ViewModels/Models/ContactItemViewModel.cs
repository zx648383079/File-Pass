using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ContactItemViewModel: BindableBase
    {

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;


        private string _lastMessage = string.Empty;

        public string LastMessage {
            get => _lastMessage;
            set => Set(ref _lastMessage, value);
        }

        private DateTime? _lastAt;

        public DateTime? LastAt {
            get => _lastAt;
            set => Set(ref _lastAt, value);
        }


        private int _messageCount;

        public int MessageCount {
            get => _messageCount;
            set => Set(ref _messageCount, value);
        }

    }
}
