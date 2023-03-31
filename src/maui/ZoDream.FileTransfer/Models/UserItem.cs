using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Models
{
    [Serializable()]
    public class UserItem: BindableObject, IUser
    {
        public string Id { get; set; } = string.Empty;

        public string Avatar { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        private string markName = string.Empty;

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

        public int EncryptType { get; set; }

        public string EncryptRule { get; set; } = string.Empty;


        public Color? AvatarBackground { get; set; }

        public MessageItem Message {
            set {
                var msg = value.ToShortMessage();
                if (string.IsNullOrWhiteSpace(msg))
                {
                    return;
                }
                UnreadCount++;
                LastMessage = msg;
                LastAt = value.CreatedAt;
            }
        }

        public UserItem()
        {
            
        }

        public UserItem(IUser item)
        {
            Update(item);
        }

        public void Update(IUser item)
        {
            Id = item.Id;
            if (!string.IsNullOrWhiteSpace(item.Avatar) || string.IsNullOrWhiteSpace(Avatar))
            {
                Avatar = string.IsNullOrWhiteSpace(item.Avatar) ? UserInfoItem.RandomAvatar() : item.Avatar;
            }
            Name = item.Name;
            Ip = item.Ip;
            Port = item.Port;
            if (string.IsNullOrWhiteSpace(MarkName))
            {
                MarkName = Name;
            }
        }
    }
}
