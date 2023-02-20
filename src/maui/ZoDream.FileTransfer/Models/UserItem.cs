using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Models
{
    [Serializable()]
    public class UserItem: BindableObject
    {
        public string Id { get; set; }

        public string Avatar { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        private string markName;

        public string MarkName
        {
            get { return markName; }
            set { 
                markName = value;
                OnPropertyChanged();
            }
        }


        [NonSerialized()]
        private bool online = false;

        public bool Online
        {
            get { return online; }
            set {
                online = value;
                OnPropertyChanged();
            }
        }

        [NonSerialized()]
        private string lastMessage = string.Empty;

        public string LastMessage
        {
            get { return lastMessage; }
            set { 
                lastMessage = value;
                OnPropertyChanged();
            }
        }

        [NonSerialized()]
        private DateTime lastAt = DateTime.MinValue;

        public DateTime LastAt
        {
            get { return lastAt; }
            set { 
                lastAt = value;
                OnPropertyChanged();
            }
        }

        [NonSerialized()]
        private int unreadCount = 0;

        public int UnreadCount
        {
            get { return unreadCount; }
            set { 
                unreadCount = value;
                OnPropertyChanged();
            }
        }


        public Color AvatarBackground { get; set; }


        public UserItem()
        {
            
        }

        public UserItem(UserInfoItem item)
        {
            Id = item.Id;
            Avatar = item.Avatar;
            Name = item.Name;
            Ip = item.Ip;
            Port = item.Port;
            MarkName = Name;
        }
    }
}
