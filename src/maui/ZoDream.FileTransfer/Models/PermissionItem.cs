using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class PermissionItem
    {
        public string Icon { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;


        public PermissionItem()
        {
            
        }

        public PermissionItem(string icon, string name, string description)
        {
            Icon = icon;
            Name = name;
            Description = description;
        }


        public static IList<PermissionItem> PermissionItems = new List<PermissionItem>()
        {
            new PermissionItem("\ue696", "文件读写权限", "接收和发送文件"),
            new PermissionItem("\ue6ab", "网络权限", "接收和发送消息"),
            new PermissionItem("\ue6e0", "麦克风权限", "发送语音消息"),
            new PermissionItem("\ue639", "摄像头权限", "发送视频消息"),
        };
    }
}
