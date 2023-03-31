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

    public interface IClientToken: IClientAddress
    {
        public string Id { get; }
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

    public class ClientToken : ClientAddress, IClientToken
    {
        public ClientToken(string ip, int port): base(ip, port)
        {
            Ip = ip;
            Port = port;
        }

        public ClientToken(string ip, int port, string token): base(ip, port)
        {
            Id = token;
        }

        public string Id { get; set; } = string.Empty;
    }
}
