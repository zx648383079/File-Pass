using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class MessageMoreItem
    {
        public string Label { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public MessageMoreItem()
        {

        }

        public MessageMoreItem(string name, string label, string icon)
        {
            Name = name;
            Label = label;
            Icon = icon;
        }
    }
}
