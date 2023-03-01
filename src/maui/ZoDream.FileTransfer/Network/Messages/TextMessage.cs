using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ZoDream.FileTransfer.Network.Messages
{
    public class TextMessage : StringMessage
    {
        public string Data { get; set; }

        protected override void FromStr(string val)
        {
            Data = val;
        }

        protected override string ToStr()
        {
            return Data;
        }
    }
}
