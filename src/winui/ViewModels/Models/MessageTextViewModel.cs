using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.ViewModels
{
    public class MessageTextViewModel(string name, string avatar, string content): MessageItemWithUser(name, avatar)
    {

        public string Content { get; private set; } = content;
    }
}
