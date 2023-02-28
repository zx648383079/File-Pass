using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class UserInfoItem: IUser
    {
        public string Id { get; set; }

        public string Avatar { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        public UserInfoItem()
        {
            
        }

        public UserInfoItem(IUser item)
        {
            Id = item.Id;
            Avatar = item.Avatar;
            Name = item.Name;
            Ip = item.Ip;
            Port = item.Port;
        }
    }

    public class UserInfoOption: UserInfoItem, INotifyPropertyChanged
    {

        private bool isChecked;

        public bool IsChecked {
            get { return isChecked; }
            set { 
                isChecked = value;
                OnPropertyChanged();
            }
        }


        private int status;
        /// <summary>
        /// 状态，0 待处理 1 申请中 2 已同意 3 已拒绝
        /// </summary>
        public int Status
        {
            get { return status; }
            set { 
                status = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public UserInfoOption()
        {
            
        }

        public UserInfoOption(IUser user): base(user)
        {
            
        }
    }
}
