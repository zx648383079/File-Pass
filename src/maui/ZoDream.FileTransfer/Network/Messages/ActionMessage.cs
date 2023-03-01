using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network.Messages
{
    public class ActionMessage : StringMessage
    {
        public string MessageId { get; set; }

        public MessageTapEvent EventType { get; set; }

        protected override void FromStr(string val)
        {
            var arg = val.Split(',');
            MessageId = arg[0];
            EventType = (MessageTapEvent)Convert.ToInt32(arg[1]);
        }

        protected override string ToStr()
        {
            return $"{MessageId},{(int)EventType}";
        }
    }
}
