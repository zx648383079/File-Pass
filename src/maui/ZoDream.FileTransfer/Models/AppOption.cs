using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    internal class AppOption: UserInfoItem
    {

        public bool IsHideClient { get; set; }

        public bool IsOpenLink { get; set; }

        public bool IsSaveFile { get; set; }

        public UserInfoItem FormatInfo()
        {
            return new UserInfoItem()
            {
                Id = Id,
                Name = Name,
                Avatar = Avatar,
                Port = Port,
                Ip = Ip,
            };
        }
    }
}
