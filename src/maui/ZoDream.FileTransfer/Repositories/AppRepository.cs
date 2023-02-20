using System.Text;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Repositories
{
    internal class AppRepository : IDisposable
    {
        public AppRepository()
        {
            _ = LoadAsync();
        }

        public bool Booted { get; private set; } = false;

        private string SecureKey = string.Empty;

        public FileRepository FileHub { get; private set; }

        public SocketHub NetHub { get; private set; }

        public AppOption Option { get; private set; }

        public Dictionary<string, UserInfoItem> CacheItems { get; private set; } = new();
        public IList<UserItem> UserItems { get; private set; }

        public event UsersUpdatedEventHandler UsersUpdated;
        public event NewUserEventHandler NewUser;
        public event NewMessageEventHandler NewMessage;

        #region 联系人相关

        public void Add(UserItem item)
        {
            if (IndexOf(item.Id) >= 0)
            {
                return;
            }
            UserItems.Add(item);
        }

        public void Add(UserInfoItem item)
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

        public Task WaitBoot()
        {
            return Task.Factory.StartNew(() =>
            {
                if (Booted)
                {
                    return;
                }
                while (!Booted)
                {
                    
                }
            });
        }

        public async Task<IList<UserItem>> LoadUsersAsync()
        {
            var items = await FileHub.ReadAsync<IList<UserItem>>(Constants.USERS_FILE);
            return items is null ? new List<UserItem>() : items;
        }

        public async Task SaveUsersAsync(IList<UserItem> items)
        {
            await FileHub.WriteAsync(Constants.USERS_FILE, items);
        }

        public async Task<IList<MessageItem>> LoadMessageAsync(UserItem user)
        {
            return await FileHub.ReadAsync<IList<MessageItem>>(
                $"{Constants.MESSAGE_FOLDER}/{user.Id}.db"
                );
        }

        public async Task SaveOptionAsync()
        {
            await FileHub.WriteAsync(Constants.OPTION_FILE, Option);
        }

        public async Task SaveMessageAsync(UserItem user, IList<MessageItem> items)
        {
            await FileHub.MakeFolderAsync(Constants.MESSAGE_FOLDER);
            await FileHub.WriteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db", items);
        }

        public async Task RemoveUserAsync(UserItem user)
        {
            await FileHub.DeleteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db");
        }

        public async Task LoadAsync()
        {
            // 用户数据加密Key
            SecureKey = await LoadKeyAsync();
            // 
            FileHub = new FileRepository(FileSystem.Current.CacheDirectory,
                Encoding.UTF8.GetBytes(SecureKey), Encoding.UTF8.GetBytes(Constants.AES_IV));
            Option = await LoadOptionAsync();
            UserItems = await LoadUsersAsync();
            NetHub = new SocketHub();
            NetHub.MessageReceived += NetHub_MessageReceived;
            if (!Option.IsHideClient)
            {
                NetHub.Listen(Option.Ip, Option.Port);
            }
            Booted = true;
            UsersUpdated?.Invoke();
        }

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
                        Data = Option.FormatInfo()
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
        public async Task<bool> SendTextAsync(string id, string content)
        {
            var user = Get(id);
            return await NetHub.SendAsync(user, new TextMessage() { Text = content, 
                Type = SocketMessageType.String });
        }
        #endregion

        #region 用户数据处理方法

        private async Task<AppOption> LoadOptionAsync()
        {
            var data = await FileHub.ReadAsync<AppOption>(Constants.OPTION_FILE);
            data ??= new AppOption()
            {
                Name = DeviceInfo.Current.Name,
                // 生成独一无二的本机标识,方便动态IP重新连接
                Id = Str.MD5Encode($"{DeviceInfo.Current.Name}_{DateTime.Now.Ticks}"),
                Port = Constants.DEFAULT_PORT
            };
            if (string.IsNullOrWhiteSpace(data.Ip) || data.Ip.StartsWith("192.168"))
            {
                data.Ip = Ip.Get();
            }
            return data;
        }

        private async Task<string> LoadKeyAsync()
        {
            var secureKey = await SecureStorage.Default.GetAsync(Constants.SECURE_KEY);
            if (!string.IsNullOrEmpty(secureKey))
            {
                return secureKey;
            }
            secureKey = Str.MD5Encode($"{DeviceInfo.Current.Platform}{DeviceInfo.Current.Name}{DateTime.Now}");
            await SecureStorage.Default.SetAsync(Constants.SECURE_KEY, secureKey);
            return secureKey;
        }



        #endregion

        /// <summary>
        /// 请求权限
        /// </summary>
        /// <returns></returns>
        public async Task<PermissionStatus> CheckAndRequestNetworkPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.NetworkState>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.NetworkState>();

            return status;
        }

        public void Dispose()
        {
            NetHub?.Dispose();
            FileHub?.Dispose();
        }

        
    }
}
