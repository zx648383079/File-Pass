using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.Network.Messages
{
    public class IpMessage : StringMessage, IClientAddress
    {
        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        protected override void FromStr(string val)
        {
            var i = val.LastIndexOf(':');
            if (i < 0)
            {
                Ip = val;
                Port = Constants.DEFAULT_PORT;
            } else
            {
                Ip = val[..i];
                Port = Convert.ToInt32(val[(i+1)..]);
            }
        }

        protected override string ToStr()
        {
            return $"{Ip}:{Port}";
        }
    }
}
