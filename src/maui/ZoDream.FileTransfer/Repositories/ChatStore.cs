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

        public IList<UserItem> UserItems { get; private set; }
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
            App.DataHub.AddUserAsync(item);
            UserItems.Add(new UserItem(item));
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


        private void NetHub_MessageReceived(SocketClient client, string ip, int port, MessageEventArg message)
        {
            var user = Get(ip, port);
            App.Logger.Debug($"Receive<ip[{ip}:{port}]>:user[{user?.Name}:{user?.Id}]");
            var net = App.NetHub;
            MessageItem? msg = null; 
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
                case SocketMessageType.UserAddResponse:
                    foreach (var item in ApplyItems)
                    {
                        if (item.Ip == ip && item.Port == port)
                        {
                            if ((message.Data as BoolMessage).Data)
                            {
                                Add(item);
                            }
                            ApplyItems.Remove(user);
                            return;
                        }
                    }
                    break;
                case SocketMessageType.MessageText:
                    msg = new TextMessageItem()
                    {
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        Content = (message.Data as TextMessage).Data,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    App.DataHub.AddMessageAsync(user, msg);
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.Close:
                case SocketMessageType.MessagePing:
                    msg = new ActionMessageItem(message.EventType)
                    {
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    if (message.EventType == SocketMessageType.MessagePing)
                    {
                        App.DataHub.AddMessageAsync(user, msg);
                    }
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.MessageFile:
                    var file = message.Data as FileMessage;
                    msg = new FileMessageItem()
                    {
                        Id = file.MessageId,
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        FileName = file.FileName,
                        Size = file.Length,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    App.DataHub.AddMessageAsync(user, msg);
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.MessageFolder:
                    var folder = message.Data as FileMessage;
                    msg = new FolderMessageItem()
                    {
                        Id = folder.MessageId,
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        FolderName = folder.FileName,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    App.DataHub.AddMessageAsync(user, msg);
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.MessageSync:
                    var sync = message.Data as FileMessage;
                    msg = new SyncMessageItem()
                    {
                        Id = sync.MessageId,
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        FolderName = sync.FileName,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    App.DataHub.AddMessageAsync(user, msg);
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.MessageUser:
                    var u = message.Data as UserMessage;
                    msg = new UserMessageItem()
                    {
                        UserId = user.Id,
                        ReceiveId = App.Option.Id,
                        Data = u.Data,
                        IsSender = false,
                        CreatedAt = DateTime.Now,
                        IsSuccess = true,
                    };
                    App.DataHub.AddMessageAsync(user, msg);
                    NewMessage?.Invoke(user.Id, msg);
                    break;
                case SocketMessageType.MessageAction:
                    var action = message.Data as ActionMessage;
                    if (action.EventType == MessageTapEvent.Withdraw)
                    {
                        App.DataHub.RemoveMessageAsync(new TextMessageItem()
                        {
                            Id = action.MessageId,
                            IsSender = false,
                            UserId = user.Id,
                            ReceiveId = App.Option.Id,
                            CreatedAt = DateTime.MinValue
                        });
                    }
                    MessageUpdated?.Invoke(action.MessageId, action.EventType, null);
                    break;
                case SocketMessageType.RequestSpecialLine:
                    var act = message.Data as ActionMessage;
                    if (ConfirmItems.TryGetValue(act.MessageId, out var mess))
                    {
                        var link = App.NetHub.Connect(ip, port);
                        if (client is null)
                        {
                            // 失败
                            return;
                        }
                        SendConfirmedMessage(link, mess);
                    }
                    break;
                case SocketMessageType.SpecialLine:
                    var txt = message.Data as TextMessage;
                    if (ConfirmItems.TryGetValue(txt.Data, out var mes))
                    {
                        SendConfirmedMessage(client, mes, true);
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
                ReceiveId = user.Id,
                UserId = App.Option.Id,
                IsSender = true,
                Content = content,
                CreatedAt = DateTime.Now,
                IsSuccess = false
            };
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.MessageText, new TextMessage()
            {
                Data = content,
            });
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
            message.IsSuccess = await App.NetHub.SendAsync(user, SocketMessageType.Ping, null);
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
                new FileMessage()
            {
                FileName = message.FileName,
                MessageId = message.Id,
                Length = message.Size
            });
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
                new FileMessage()
                {
                    FileName = message.FileName,
                    MessageId = message.Id,
                    Length = 0
                });
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
                new FileMessage()
                {
                    FileName = message.FileName,
                    MessageId = message.Id,
                    Length = 0
                });
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
                new UserMessage()
                {
                    Data = data
                });
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
