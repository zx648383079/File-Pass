using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.Shared.Net
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
