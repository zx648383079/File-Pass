using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Network.Messages;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Repositories
{
    public class ChatStore
    {
        public ChatStore(AppRepository app) 
        {
            App = app;
        }

        private readonly AppRepository App;

        public IList<UserItem> UserItems { get; private set; } = new List<UserItem>();
        /// <summary>
        /// 以确认的消息，允许后台进行操作，例如文件接收和发送
        /// </summary>
        public Dictionary<string, MessageItem> ConfirmItems = new();
        public Dictionary<string, IMessageSocket> LinkItems = new();
        public List<IUser> ApplyItems = new();

        public event UsersUpdatedEventHandler? UsersUpdated;
        public event NewUserEventHandler? NewUser;
        public event NewMessageEventHandler? NewMessage;
        public event MessageUpdatedEventHandler? MessageUpdated;

        public async Task InitializeAsync()
        {
            UserItems = await App.DataHub.GetUsersAsync();
            UsersUpdated?.Invoke();
            var net = App.NetHub;
            net.MessageReceived += NetHub_MessageReceived;
            if (App.Option.Ip == Constants.LOCALHOST)
            {
                App.Logger.Error($"Client IP Error: {App.Option.Ip}");
                return;
            }
            net.Udp.Listen(App.Option.Ip, Constants.DEFAULT_PORT);
            if (!App.Option.IsHideClient)
            {
                net.Tcp.Listen(App.Option.Ip, App.Option.Port);
                net.Ping(App.Option);
            } else
            {
                net.Ping(UserItems, App.Option);
            }
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
            App.DataHub.AddUserAsync(item);
            UsersUpdated?.Invoke();
        }

        public void Remove(IUser item)
        {
            var isUpdated = false;
            for (int i = UserItems.Count - 1; i >= 0; i--)
            {
                if (UserItems[i].Id == item.Id)
                {
                    UserItems.RemoveAt(i);
                    isUpdated = true;
                }
            }
            if (isUpdated)
            {
                App.DataHub.RemoveUserAsync(item);
            }
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

        public UserItem? Get(string id)
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

        public UserItem? Get(string ip, int port)
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


        private void NetHub_MessageReceived(SocketClient? client, string ip, int port, MessageEventArg message)
        {
            var user = Get(ip, port);
            if (user != null)
            {
                user.Online = true;
            }
            App.Logger.Debug($"Receive<ip[{ip}:{port}]user[{user?.Name}]>:{message.EventType}->{message.IsRequest}");
            var net = App.NetHub;
            switch (message.EventType)
            {
                case SocketMessageType.Ping:
                    var remote = (message.Data as UserMessage)!.Data;
                    if (remote.Id == App.Option.Id)
                    {
                        break;
                    }
                    var hasUser = IndexOf(remote.Id) >= 0;
                    if (message.IsRequest && (!App.Option.IsHideClient || hasUser))
                    {
                        net.ResponsePing(ip, port, App.Option);
                    }
                    if (hasUser)
                    {
                        break;
                    }
                    NewUser?.Invoke(remote, false);
                    break;
                case SocketMessageType.UserAddRequest:
                    var requestUser = (message.Data as UserMessage)!.Data;
                    if (requestUser is null)
                    {
                        break;
                    }
                    var existUser = Get(requestUser.Id);
                    if (existUser is not null)
                    {
                        existUser.Update(requestUser);
                    }
                    if (existUser is not null || App.Option.IsOpenLink)
                    {
                        _ = AgreeAddUserAsync(requestUser, true);
                        break;
                    }
                    NewUser?.Invoke(requestUser, true);
                    break;
                case SocketMessageType.UserAddResponse:
                    var isAgree = (message.Data as BoolMessage)!.Data;
                    foreach (var item in ApplyItems)
                    {
                        if (item.Ip == ip && item.Port == port)
                        {
                            if (isAgree)
                            {
                                Add(item);
                            }
                            ApplyItems.Remove(item);
                            NewUser?.Invoke(item, true);
                            return;
                        }
                    }
                    break;
                case SocketMessageType.MessageText:
                case SocketMessageType.Close:
                case SocketMessageType.MessagePing:
                case SocketMessageType.MessageFile:
                case SocketMessageType.MessageFolder:
                case SocketMessageType.MessageSync:
                case SocketMessageType.MessageUser:
                    AddMessage(message.EventType, message.Data, user);
                    break;
                case SocketMessageType.MessageAction:
                    var action = message.Data as ActionMessage;
                    if (action!.EventType == MessageTapEvent.Withdraw)
                    {
                        App.DataHub.RemoveMessageAsync(new TextMessageItem()
                        {
                            Id = action.MessageId,
                            IsSender = false,
                            UserId = user!.Id,
                            ReceiveId = App.Option.Id,
                            CreatedAt = DateTime.MinValue
                        });
                    }
                    MessageUpdated?.Invoke(action.MessageId, action.EventType, null);
                    break;
                case SocketMessageType.RequestSpecialLine:
                    var act = message.Data as ActionMessage;
                    if (ConfirmItems.TryGetValue(act!.MessageId, out var mess))
                    {
                        var link = SocketHub.Connect(ip, port);
                        if (link is null)
                        {
                            // 失败
                            App.Logger.Error($"Request Special Line Error: {ip}:{port}");
                            return;
                        }
                        SendConfirmedMessage(link, mess);
                    }
                    break;
                case SocketMessageType.SpecialLine:
                    var txt = message.Data as TextMessage;
                    if (ConfirmItems.TryGetValue(txt!.Data, out var mes))
                    {
                        SendConfirmedMessage(client!, mes, true);
                    }
                    break;
                default:
                    break;
            }
        }

        private void AddMessage(SocketMessageType type, IMessageUnpack? data, IUser? user)
        {
            if (user is null || data is null)
            {
                return;
            }
            var msg = RenderMessage(type);
            if (msg is null)
            {
                return;
            }
            msg.UserId = user.Id;
            msg.ReceiveId = App.Option.Id;
            msg.IsSender = false;
            msg.CreatedAt = DateTime.Now;
            msg.IsSuccess = true;
            msg.ReadFrom(data);
            if (type != SocketMessageType.Close)
            {
                App.DataHub.AddMessageAsync(user, msg);
            }
            if (user is UserItem u)
            {
                u.Message = msg;
            }
            NewMessage?.Invoke(user.Id, msg);
        }

        private static MessageItem? RenderMessage(SocketMessageType type)
        {
            return type switch
            {
                SocketMessageType.MessageText => new TextMessageItem(),
                SocketMessageType.MessageFile => new FileMessageItem(),
                SocketMessageType.MessageFolder => new FolderMessageItem(),
                SocketMessageType.MessageSync => new SyncMessageItem(),
                SocketMessageType.MessagePing or SocketMessageType.Close => new ActionMessageItem(type),
                _ => null,
            };
        }

        public void AddConfirmMessage(MessageItem message)
        {
            ConfirmItems.Add(message.Id, message);
        }

        public void AddConfirmMessage(IUser user, MessageItem message)
        {
            AddConfirmMessage(message);
            var client = SocketHub.Connect(user.Ip, user.Port);
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

        private void SendConfirmedMessage(SocketClient client, 
            MessageItem message, bool hasHeader = false)
        {
            if (!hasHeader)
            {
                client.Send(SocketMessageType.SpecialLine);
                client.Send(new TextMessage()
                {
                    Data = message.Id
                });
            }
            IMessageSocket? link = null;
            if (message is SyncMessageItem sync)
            {
                sync.Status = FileMessageStatus.Transferring;
                link = new SyncMessageSocket(client, sync.Id, sync.LocationFolder);
                link.OnProgress += (_, fileName, p, t) => {
                    sync.FileName = fileName;
                    sync.Size = t;
                    sync.Progress = p;
                };
                link.OnCompleted += (_, _, suc) => {
                    sync.Status = suc ? FileMessageStatus.Success : FileMessageStatus.Failure;
                };
            } else if (message is FolderMessageItem folder)
            {
                folder.Status = FileMessageStatus.Transferring;
                link = new FolderMessageSocket(client, folder.Id, folder.LocationFolder);
                link.OnProgress += (_, fileName, p, t) => {
                    folder.FileName = fileName;
                    folder.Size = t;
                    folder.Progress = p;
                };
                link.OnCompleted += (_, _, suc) => {
                    folder.Status = suc ? FileMessageStatus.Success : FileMessageStatus.Failure;
                };
            }
            else if (message is FileMessageItem file)
            {
                file.Status = FileMessageStatus.Transferring;
                link = new FileMessageSocket(client, file.Id,
                    file.FileName, file.Location);
                link.OnProgress += (_, _, p, t) => {
                    file.Progress = p;
                };
                link.OnCompleted += (_, _, suc) => {
                    file.Status = suc ? FileMessageStatus.Success : FileMessageStatus.Failure;
                };
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
            if (LinkItems.TryGetValue(message.Id, out IMessageSocket? value))
            {
                value.Dispose();
                LinkItems.Remove(message.Id);
            }
            if (message is FileMessageItem file)
            {
                file.Status = FileMessageStatus.Canceled;
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
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                IsSender = true,
                Content = content,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, 
                SocketMessageType.MessageText, 
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
            return message;
        }

        public async Task<MessageItem> SendPingAsync(IUser user)
        {
            var message = new ActionMessageItem(SocketMessageType.MessagePing)
            {
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                IsSender = true,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.Ping,
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
            return message;
        }

        public async Task<MessageItem> SendFileAsync(IUser user, string fileName, string path)
        {
            var message = new FileMessageItem()
            {
                IsSender = true,
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                Id = GenerateMessageId(),
                FileName = fileName,
                Size = await App.Storage.GetSizeAsync(path),
                Location = path,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.MessageFile,
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
            AddConfirmMessage(message);
            return message;
        }

        public async Task<MessageItem> SendFolderAsync(IUser user, string folderName, 
            string path)
        {
            var message = new FolderMessageItem()
            {
                IsSender = true,
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                Id = GenerateMessageId(),
                FolderName = folderName,
                LocationFolder = path,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, 
                SocketMessageType.MessageFolder,
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
            AddConfirmMessage(message);
            return message;
        }

        public async Task<MessageItem> SendSyncAsync(IUser user, string folderName,
            string path)
        {
            var message = new SyncMessageItem()
            {
                IsSender = true,
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                Id = GenerateMessageId(),
                FolderName = folderName,
                LocationFolder = path,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user,
                SocketMessageType.MessageSync,
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
            AddConfirmMessage(message);
            return message;
        }

        public async Task<MessageItem> SendUserAsync(IUser user, IUser data)
        {
            var message = new UserMessageItem()
            {
                IsSender = true,
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                Id = GenerateMessageId(),
                Data = data,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user,
                SocketMessageType.MessageUser,
                message.WriteTo());
            _ = App.DataHub.AddMessageAsync(user, message);
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
            var res = await App.NetHub.SendAsync(user, SocketMessageType.MessageAction,
                new ActionMessage()
                {
                    MessageId = data.Id,
                    EventType = MessageTapEvent.Withdraw,
                });
            if (res)
            {
                _ = App.DataHub.RemoveMessageAsync(data);
            }
            return res;
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
                ApplyItems.Remove(user);
            }
            return yes;
        }

        public async Task<bool> AddUserAsync(IUser user)
        {
            if (IndexOf(user.Id) >= 0 || ApplyItems.IndexOf(user) >= 0)
            {
                return true;
            }
            var res = await App.NetHub.SendAsync(user, SocketMessageType.UserAddRequest, new UserMessage()
            {
                Data = App.Option
            });
            if (res)
            {
                ApplyItems.Add(user);
            }
            return res;
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
