using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class AppOption: UserInfoItem
    {
        /// <summary>
        /// 启动隐藏模式，关闭主动监听模式，不允许其他设备发现本机！
        /// </summary>
        public bool IsHideClient { get; set; }
        /// <summary>
        /// 当新设备连接本机时，自动同意
        /// </summary>
        public bool IsOpenLink { get; set; }
        /// <summary>
        /// 当收到文件时自动同意接收并保存文件
        /// </summary>
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
