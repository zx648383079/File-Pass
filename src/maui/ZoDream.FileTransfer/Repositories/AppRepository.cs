using System.Text;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;
using static Android.Content.ClipData;

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

        public FileRepository Repository { get; private set; }

        public SocketHub NetHub { get; private set; }

        public AppOption Option { get; private set; }

        public IList<UserItem> UserItems { get; private set; }

        #region 联系人相关

        public void Add(UserItem item)
        {
            UserItems.Add(item);
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
            return await Repository.ReadAsync<IList<UserItem>>(Constants.USERS_FILE);
        }

        public async Task SaveUsersAsync(IList<UserItem> items)
        {
            await Repository.WriteAsync(Constants.USERS_FILE, items);
        }

        public async Task<IList<MessageItem>> LoadMessageAsync(UserItem user)
        {
            return await Repository.ReadAsync<IList<MessageItem>>(
                $"{Constants.MESSAGE_FOLDER}/{user.Id}.db"
                );
        }

        public async Task SaveOptionAsync()
        {
            await Repository.WriteAsync(Constants.OPTION_FILE, Option);
        }

        public async Task SaveMessageAsync(UserItem user, IList<MessageItem> items)
        {
            await Repository.MakeFolder(Constants.MESSAGE_FOLDER);
            await Repository.WriteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db", items);
        }

        public async Task RemoveUserAsync(UserItem user)
        {
            await Repository.DeleteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db");
        }

        public async Task LoadAsync()
        {
            // 用户数据加密Key
            SecureKey = await LoadKeyAsync();
            // 
            Repository = new FileRepository(FileSystem.Current.CacheDirectory,
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
        }

        private void NetHub_MessageReceived(string ip, ISocketMessage message)
        {
            
        }

        #region 用户数据处理方法

        private async Task<AppOption> LoadOptionAsync()
        {
            var data = await Repository.ReadAsync<AppOption>(Constants.OPTION_FILE);
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
            Repository?.Dispose();
        }

        
    }
}
