using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Network.Messages;
using ZoDream.FileTransfer.Utils;
using static CoreFoundation.DispatchSource;

namespace ZoDream.FileTransfer.Repositories
{
    public class ChatStore
    {
        public ChatStore(AppRepository app) 
        {
            App = app;
        }

        private readonly AppRepository App;

        public Dictionary<string, UserInfoItem> CacheItems { get; private set; } = new();
        public IList<UserItem> UserItems { get; private set; }
        /// <summary>
        /// 以确认的消息，允许后台进行操作，例如文件接收和发送
        /// </summary>
        public Dictionary<string, MessageItem> ConfirmItems = new();
        public Dictionary<string, IMessageSocket> LinkItems = new();

        public event UsersUpdatedEventHandler UsersUpdated;
        public event NewUserEventHandler NewUser;
        public event NewMessageEventHandler NewMessage;
        public event MessageUpdatedEventHandler MessageUpdated;

        public async Task InitializeAsync()
        {
            UserItems = await App.DataHub.GetUsersAsync();
            var net = App.NetHub;
            net.MessageReceived += NetHub_MessageReceived;
            net.Udp.Listen(App.Option.Ip, Constants.DEFAULT_PORT);
            if (!App.Option.IsHideClient)
            {
                net.Tcp.Listen(App.Option.Ip, App.Option.Port);
                net.Ping(App.Option);
            } else
            {
                net.Ping(UserItems, App.Option);
            }
            UsersUpdated?.Invoke();
        }

        #region 联系人相关

        public string GenerateId()
        {
            return Str.MD5Encode($"{DeviceInfo.Current.Name}_{DateTime.Now.Ticks}");
        }

        public void Add(UserItem item)
        {
            if (IndexOf(item.Id) >= 0)
            {
                return;
            }
            UserItems.Add(item);
        }


        public void Add(IUser item)
        {
            if (IndexOf(item.Id) >= 0)
            {
                return;
            }
            UserItems.Add(new UserItem(item));
        }

        public int IndexOf(UserItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                return IndexOfIp(item.Ip);
            }
            for (int i = 0; i < UserItems.Count; i++)
            {
                if (item.Id == UserItems[i].Id)
                {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOf(string userId)
        {
            for (int i = 0; i < UserItems.Count; i++)
            {
                if (userId == UserItems[i].Id)
                {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOf(string ip, int port)
        {
            for (int i = 0; i < UserItems.Count; i++)
            {
                if (ip == UserItems[i].Ip && port == UserItems[i].Port)
                {
                    return i;
                }
            }
            return -1;
        }


        public int IndexOfIp(string ip)
        {
            for (int i = 0; i < UserItems.Count; i++)
            {
                if (ip == UserItems[i].Ip)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool ContainsIp(string ip)
        {
            return IndexOfIp(ip) >= 0;
        }

        public UserItem Get(string id)
        {
            foreach (var item in UserItems)
            {
                if (id == item.Id)
                {
                    return item;
                }
            }
            return null;
        }

        public UserItem Get(string ip, int port)
        {
            foreach (var item in UserItems)
            {
                if (ip == item.Ip && port == item.Port)
                {
                    return item;
                }
            }
            return null;
        }

        #endregion


        private void NetHub_MessageReceived(string ip, int port, MessageEventArg message)
        {
            var user = Get(ip, port);
            var net = App.NetHub;
            switch (message.EventType)
            {
                case SocketMessageType.Ping:
                    if (message.IsRequest)
                    {
                        net.ResponsePing(ip, port, App.Option);
                    }
                    NewUser?.Invoke((message.Data as UserMessage).Data);
                    break;
                case SocketMessageType.UserAddRequest:
                    NewUser?.Invoke((message.Data as UserMessage).Data);
                    break;
                case SocketMessageType.MessageText:
                    NewMessage?.Invoke(user.Id, new TextMessageItem() {
                        Content = (message.Data as TextMessage).Data,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    });
                    break;
                case SocketMessageType.Close:
                case SocketMessageType.MessagePing:
                    NewMessage?.Invoke(user.Id, new ActionMessageItem(message.EventType)
                    {
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    });
                    break;
                case SocketMessageType.MessageFile:
                    var file = message.Data as FileMessage;
                    NewMessage?.Invoke(user.Id, new FileMessageItem()
                    {
                        Id = file.MessageId,
                        FileName = file.FileName,
                        Size = file.Length,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    });
                    break;
                case SocketMessageType.MessageAction:
                    var action = message.Data as ActionMessage;
                    MessageUpdated?.Invoke(action.MessageId, action.EventType, null);
                    break;
                case SocketMessageType.RequestSpecialLine:
                    var act = message.Data as ActionMessage;
                    if (ConfirmItems.TryGetValue(act.MessageId, out var mess))
                    {
                        var client = App.NetHub.Connect(ip, port);
                        if (client is null)
                        {
                            // 失败
                            return;
                        }
                        SendConfirmedMessage(client, mess);
                    }
                    break;
                default:
                    break;
            }
        }

        public void AddConfirmMessage(MessageItem message)
        {
            ConfirmItems.Add(message.Id, message);
        }

        public void AddConfirmMessage(IUser user, MessageItem message)
        {
            AddConfirmMessage(message);
            var client = App.NetHub.Connect(user.Ip, user.Port);
            if (client is null)
            {
                // 无法创建连接，发送消息给对面，让对方创建
                _ = App.NetHub.SendAsync(user,
                    SocketMessageType.RequestSpecialLine, new ActionMessage()
                    {
                        EventType = MessageTapEvent.None,
                        MessageId = message.Id,
                    });
                return;
            }
            SendConfirmedMessage(client, message);
        }

        private void SendConfirmedMessage(SocketClient client, MessageItem message)
        {
            
            IMessageSocket link = null;
            if (message is FileMessageItem file)
            {
                link = new FileMessageSocket(client, file.Id, 
                    file.FileName, file.Location, (p,l) => {
                        file.Size = l;
                        file.Progress = p;
                    });
            }
            if (link is null)
            {
                return;
            }
            LinkItems.Add(message.Id, link);
            if (!message.IsSender)
            {
                // 接收
                link.ReceiveAsync();
                return;
            } else
            {
                link.SendAsync();
            }
            
        }

        public void RemoveConfirmMessage(MessageItem message)
        {
            ConfirmItems.Remove(message.Id);
            if (LinkItems.TryGetValue(message.Id, out IMessageSocket value))
            {
                value.Dispose();
                LinkItems.Remove(message.Id);
            }
        }

        #region 发送消息
        /// <summary>
        /// 生成一个消息id
        /// </summary>
        /// <returns></returns>
        public string GenerateMessageId()
        {
            return Str.MD5Encode($"{App.Option.Id}_{DateTime.Now.Ticks}");
        }

        public async Task<MessageItem> SendTextAsync(IUser user, string content)
        {
            var message = new TextMessageItem()
            {
                IsSender = true,
                Content = content,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.MessageText, new TextMessage()
            {
                Data = content,
            });
            return message;
        }

        public async Task<MessageItem> SendPingAsync(IUser user)
        {
            var message = new ActionMessageItem(SocketMessageType.Ping)
            {
                IsSender = true,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.Ping, null);
            return message;
        }

        public async Task<MessageItem> SendFileAsync(IUser user, string fileName, string path)
        {
            var message = new FileMessageItem()
            {
                IsSender = true,
                Id = GenerateMessageId(),
                FileName = fileName,
                Size = await App.Storage.GetSizeAsync(path),
                Location = path,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.MessageFile, 
                new FileMessage()
            {
                FileName = message.FileName,
                MessageId = message.Id,
                Length = message.Size
            });
            AddConfirmMessage(message);
            return message;
        }

        /// <summary>
        /// 取消消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> CancelMessageAsync(IUser user, MessageItem data)
        {
            var res = await App.NetHub.SendAsync(user, SocketMessageType.MessageAction,
                new ActionMessage()
                {
                    MessageId = data.Id,
                    EventType = MessageTapEvent.Cancel,
                });
            if (res)
            {
                RemoveConfirmMessage(data);
            }
            return res;
        }
        /// <summary>
        /// 确认消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> ConfirmMessageAsync(IUser user, MessageItem data)
        {
            var res = await App.NetHub.SendAsync(user, SocketMessageType.MessageAction,
                new ActionMessage()
                {
                    MessageId = data.Id,
                    EventType = MessageTapEvent.Confirm,
                });
            if (res)
            {
                AddConfirmMessage(user, data);
            }
            return res;
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> WithdrawMessageAsync(IUser user, MessageItem data)
        {
            return await App.NetHub.SendAsync(user, SocketMessageType.MessageAction,
                new ActionMessage()
                {
                    MessageId = data.Id,
                    EventType = MessageTapEvent.Withdraw,
                });
        }

        #endregion

        #region 界面数据调用


        public async Task<bool> AgreeAddUserAsync(IUser user, bool yes)
        {
            var res = await App.NetHub.ResponseAsync(user,
                SocketMessageType.UserAddResponse, new BoolMessage()
                {
                    Data = yes
                });
            if (!res)
            {
                return false;
            }
            if (yes)
            {
                Add(user);
            }
            return yes;
        }

        public async Task<bool> AddUserAsync(IUser user)
        {
            return await App.NetHub.SendAsync(user, SocketMessageType.UserAddRequest, new UserMessage()
            {
                Data = App.Option
            });
        }

        public Task<bool> SearchUsersAsync(string ip, int port)
        {
            return Task.Factory.StartNew(() => {
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    App.NetHub.Ping(ip, port, App.Option);
                    return true;
                }
                App.NetHub.Ping(App.Option);
                return true;
            });
        }

        #endregion
    }
}
