using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.Interfaces;
using ZoDream.Shared.ViewModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class UserItemViewModel: BindableBase, IUser
    {

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public UserItemViewModel()
        {
            
        }

        public UserItemViewModel(IUser user)
        {
            Id = user.Id;
            Name = user.Name;
            Avatar = user.Avatar;
        }
    }
}
