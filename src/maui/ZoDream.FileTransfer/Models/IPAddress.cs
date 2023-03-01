using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{

    public interface IClientAddress
    {
        public string Ip { get; }

        public int Port { get; }
    }
    public class ClientAddress: IClientAddress
    {
        public ClientAddress(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public string Ip { get; set; }

        public int Port { get; set; }
    }
}
