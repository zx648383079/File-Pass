using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageReceivedEventHandler(SocketClient client, ISocketMessage message);

    public delegate void UsersUpdatedEventHandler();
    public delegate void NewUserEventHandler(UserInfoItem user);

    public delegate void NewMessageEventHandler(string userId, MessageItem message);
}
