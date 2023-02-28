using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Repositories
{
    public class ChatStore
    {
        public ChatStore(AppRepository app) 
        {
            App = app;
        }

        private AppRepository App;

        public Dictionary<string, UserInfoItem> CacheItems { get; private set; } = new();
        public IList<UserItem> UserItems { get; private set; }
        /// <summary>
        /// 以确认的消息，允许后台进行操作，例如文件接收和发送
        /// </summary>
        public Dictionary<string, MessageItem> ConfirmItems = new();

        public event UsersUpdatedEventHandler UsersUpdated;
        public event NewUserEventHandler NewUser;
        public event NewMessageEventHandler NewMessage;

        public async Task InitializeAsync()
        {
            UserItems = await App.DataHub.GetUsersAsync();
            App.NetHub.MessageReceived += NetHub_MessageReceived;
            if (!App.Option.IsHideClient)
            {
                App.NetHub.Listen(App.Option.Ip, App.Option.Port);
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


        private void NetHub_MessageReceived(SocketClient client, ISocketMessage message)
        {
            var user = Get(client.Ip, client.Port);
            switch (message.Type)
            {
                case SocketMessageType.None:
                    break;
                case SocketMessageType.Ip:
                    break;
                case SocketMessageType.String:
                    NewMessage?.Invoke(user.Id, (message as TextMessage).ConverterTo());
                    break;
                case SocketMessageType.Numeric:
                    break;
                case SocketMessageType.Bool:
                    break;
                case SocketMessageType.Null:
                    break;
                case SocketMessageType.Ping:
                    break;
                case SocketMessageType.Close:
                    NewMessage?.Invoke(user.Id, new ActionMessageItem(message.Type));
                    break;
                case SocketMessageType.CallInfo:
                    _ = client.SendAsync(new JSONMessage<UserInfoItem>()
                    {
                        Type = SocketMessageType.Info,
                        Data = App.Option.FormatInfo()
                    });
                    break;
                case SocketMessageType.CallAddUser:
                    NewUser?.Invoke((message as JSONMessage<UserInfoItem>).Data);
                    break;
                case SocketMessageType.AddUser:
                    break;
                case SocketMessageType.FileInfo:
                    break;
                case SocketMessageType.CallFile:
                    break;
                case SocketMessageType.FilePart:
                    break;
                case SocketMessageType.FileMerge:
                    break;
                case SocketMessageType.File:
                    break;
                default:
                    break;
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
            message.IsSuccess = await App.NetHub.SendAsync(user, new TextMessage()
            {
                Text = content,
                Type = SocketMessageType.String
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
            message.IsSuccess = await App.NetHub.SendAsync(user, new NoneMessage()
            {
                Type = SocketMessageType.Ping
            });
            return message;
        }

        public async Task<MessageItem> SendFileAsync(IUser user, string fileName)
        {
            var message = new FileMessageItem()
            {
                IsSender = true,
                Id = GenerateMessageId(),
                FileName = fileName,
                Size = await App.Storage.GetSizeAsync(fileName),
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, new JSONMessage<FileInfoItem>()
            {
                Type = SocketMessageType.FileInfo,
                Data = new FileInfoItem(fileName, fileName, fileName)
            });
            return message;
        }

        /// <summary>
        /// 取消消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> CancelMessageAsync(UserItem user, MessageItem data)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 确认消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> ConfirmMessageAsync(UserItem user, MessageItem data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> WithdrawMessageAsync(UserItem user, MessageItem data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 界面数据调用


        public async Task<bool> AgreeAddUserAsync(IUser user, bool yes)
        {
            var client = App.NetHub.Connect(user.Ip, user.Port);
            if (client == null)
            {
                return false;
            }
            await client.SendAsync(new BoolMessage()
            {
                Type = SocketMessageType.AddUser,
                Value = yes
            });
            if (yes)
            {
                Add(user);
            }
            return yes;
        }

        public async Task<bool> AddUserAsync(IUser user)
        {
            var client = App.NetHub.Connect(user.Ip, user.Port);
            if (client == null)
            {
                return false;
            }
            await client.SendAsync(new JSONMessage<UserInfoItem>()
            {
                Type = SocketMessageType.CallAddUser,
                Data = App.Option.FormatInfo()
            });
            var message = await client.ReceiveAsync(SocketMessageType.AddUser);
            client.Dispose();
            if (message is BoolMessage o)
            {
                if (o.Value)
                {
                    Add(user);
                }
                return o.Value;
            }
            return false;
        }

        public async Task<IList<UserInfoOption>> SearchUsersAsync(string ip, int port)
        {
            var items = new List<UserInfoOption>();
            if (!string.IsNullOrWhiteSpace(ip))
            {
                var item = await ConnectUserAsync(ip, port);
                if (item != null)
                {
                    items.Add(item);
                }
                return items;
            }
            foreach (var item in await Utils.Ip.GetGroupOtherIpAsync())
            {
                var user = await ConnectUserAsync(item, port);
                if (user != null)
                {
                    items.Add(user);
                }
            }
            return items;
        }

        private async Task<UserInfoOption> ConnectUserAsync(string ip, int port)
        {
            var user = Get(ip, port);
            if (user != null)
            {
                return new UserInfoOption()
                {
                    Id = user.Id,
                    Name = user.Name,
                    Ip = ip,
                    Port = port,
                    Avatar = user.Avatar,
                    Status = 2,
                };
            }
            var client = App.NetHub.Connect(ip, port);
            if (client == null)
            {
                return null;
            }
            client.Send(SocketMessageType.CallInfo);
            var message = await client.ReceiveAsync(SocketMessageType.Info);
            client.Dispose();
            if (message is JSONMessage<UserInfoItem> o)
            {
                return new UserInfoOption(o.Data)
                {
                    Status = IndexOf(o.Data.Ip, o.Data.Port) < 0 ? 0 : 2,
                };
            }
            return null;
        }


        #endregion
    }
}
