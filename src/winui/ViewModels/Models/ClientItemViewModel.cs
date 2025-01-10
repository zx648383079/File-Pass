using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ClientItemViewModel: BindableBase
    {

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        private string _userName = string.Empty;

        public string UserName {
            get => _userName;
            set => Set(ref _userName, value);
        }

        private bool _isOnline;

        public bool IsOnline {
            get => _isOnline;
            set => Set(ref _isOnline, value);
        }


        private DateTime _updatedAt;

        public DateTime UpdatedAt {
            get => _updatedAt;
            set => Set(ref _updatedAt, value);
        }

        public DateTime CreatedAt { get; set; }
    }
}
