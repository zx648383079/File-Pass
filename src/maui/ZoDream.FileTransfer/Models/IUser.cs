using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public interface IUser: IClientAddress
    {
        public string Id { get; }
        public string Name { get; }
        public string Avatar { get; }
    }
}
