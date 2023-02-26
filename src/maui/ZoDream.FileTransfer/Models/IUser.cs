using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public interface IUser
    {
        public string Id { get; }
        public string Name { get; }
        public string Ip { get; }
        public int Port { get; }
        public string Avatar { get; }
    }
}
