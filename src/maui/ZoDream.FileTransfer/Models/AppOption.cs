using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    internal class AppOption
    {
        public string Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        public bool IsHideClient { get; set; }

        public bool IsOpenLink { get; set; }

        public bool IsSaveFile { get; set; }
    }
}
