using System.Text;
using ZoDream.FileTransfer.Loggers;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Repositories
{
    public class AppRepository : IDisposable
    {

        public AppRepository()
        {
            Logger = new EventLogger();
            ChatHub = new ChatStore(this);
        }

        public bool Booted { get; private set; } = false;

        private string SecureKey = string.Empty;

        public IDatabaseStore DataHub { get; private set; }

        public StorageRepository Storage { get; private set; }

        public ILogger Logger { get; private set; }

        public SocketHub NetHub { get; private set; }

        public ChatStore ChatHub { get; private set; }

        public AppOption Option { get; private set; }

        private CancellationTokenSource OptionToken = new();

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


        #region 用户数据处理方法

        public void ChangeOptionAsync(AppOption option)
        {
            Option = option;
            OptionToken.Cancel();
            OptionToken = new CancellationTokenSource();
            var token = OptionToken.Token;
            Task.Factory.StartNew(() => {
                Thread.Sleep(20 * 1000);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                if (!option.IsHideClient)
                {
                    NetHub.Tcp.Listen(option.Ip, option.Port);
                }
                DataHub.SaveOptionAsync(option);
            }, token);
            

        }

        private async Task<AppOption> LoadOptionAsync()
        {
            var data = await DataHub.GetOptionAsync();
            data ??= new AppOption()
            {
                Name = DeviceInfo.Current.Name,
                // 生成独一无二的本机标识,方便动态IP重新连接
                Id = ChatHub.GenerateId(),
                Port = Constants.DEFAULT_PORT,
                IsHideClient = true,
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

        public async Task InitializeAsync(bool isFirstLaunch) {
            // 用户数据加密Key
            Storage = new StorageRepository(FileSystem.Current.CacheDirectory);
            if (isFirstLaunch)
            {
                await Storage.InitializeAsync();
            }
            SecureKey = await LoadKeyAsync();
            Logger.Debug(SecureKey);
            // 
            DataHub = Constants.UseSQL ? new SqlStore(Storage, SecureKey)
                : new FileStore(Storage, SecureKey);
            if (isFirstLaunch) {
                await DataHub.InitializeAsync();
            }
            Option = await LoadOptionAsync();
            Logger.Debug(UserInfoItem.ToStr(Option));
            NetHub = new SocketHub();
            await ChatHub.InitializeAsync();
            Booted = true;
            Logger.Info("System Booted");
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
            DataHub?.Dispose();
        }

        
    }
}
