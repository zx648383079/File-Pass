using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.Shared.Interfaces
{
    public interface IUser
    {
        public int Id { get; }

        public string Name { get; }

        public string Avatar { get; }
    }
}
