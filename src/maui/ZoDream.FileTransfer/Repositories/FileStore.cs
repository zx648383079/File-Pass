using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Securities;

namespace ZoDream.FileTransfer.Repositories
{
    public class FileStore: IDatabaseStore
    {
        private readonly StorageRepository Storage;

        public string Password { get; private set; }

        private readonly AesSecurity Cipher;

        public FileStore(StorageRepository storage, string password) 
        {
            Storage = storage;
            Password = password;
            Cipher = new AesSecurity(password);
        }

        
        public async Task InitializeAsync() 
        {
            await Storage.MakeFolderAsync(Constants.MESSAGE_FOLDER);
        }


        public Task<AppOption?> GetOptionAsync() {
            return ReadAsync<AppOption>(Constants.OPTION_FILE);
        }

        public async Task SaveOptionAsync(AppOption option) {
            await WriteAsync(Constants.OPTION_FILE, option);
        }

        public async Task<IList<UserItem>> GetUsersAsync() {
            var items = await ReadAsync<IList<UserItem>>(Constants.USERS_FILE);
            return items is null ? new List<UserItem>() : items;
        }

        public async Task<IList<MessageItem>> GetMessagesAsync(IUser room, IUser user) {
            return await GetMessagesAsync(room.Id);
        }

        public async Task<IList<MessageItem>> GetMessagesAsync(string userId)
        {
            return (await GetMessageDataAsync(userId)).Select(i => i.ReadFrom()).ToList();
        }

        public async Task<IList<MessageFormatItem>> GetMessageDataAsync(string userId)
        {
            var items = await ReadAsync<IList<MessageFormatItem>>(
                $"{Constants.MESSAGE_FOLDER}/{userId}.db"
                );
            return items ?? new List<MessageFormatItem>();
        }

        public async Task RemoveMessageAsync(MessageItem message)
        {
            var roomId = message.IsSender ? message.ReceiveId : message.UserId;
            var items = await GetMessageDataAsync(roomId);
            var isUpdated = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (
                    (string.IsNullOrEmpty(message.Id) 
                    && items[i].CreatedAt == message.CreatedAt && 
                    message.UserId == items[i].UserId && message.ReceiveId == items[i].ReceiveId) 
                    || (!string.IsNullOrEmpty(message.Id) && items[i].Id == message.Id)
                    )
                {
                    items.RemoveAt(i);
                    isUpdated = true;
                }
            }
            if (isUpdated)
            {
                await WriteAsync($"{Constants.MESSAGE_FOLDER}/{roomId}.db", items);
            }
        }

        public async Task RemoveUserAsync(IUser user) {
            await Storage.DeleteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db");
            var items = await GetUsersAsync();
            var isUpdated = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].Id == user.Id)
                {
                    items.RemoveAt(i);
                    isUpdated = true;
                }
            }
            if (isUpdated)
            {
                await WriteAsync(Constants.USERS_FILE, items);
            }
        }

        public async Task AddUserAsync(IUser user) {
            var items = await GetUsersAsync();
            foreach (var item in items)
            {
                if (item.Id == user.Id)
                {
                    return;
                }
            }
            items.Add(user is UserItem ? (user as UserItem)! : new UserItem(user));
            await WriteAsync(Constants.USERS_FILE, items);

        }

        public async Task UpdateUserAsync(UserItem user) {
            var items = await GetUsersAsync();
            var isUpdated = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == user.Id)
                {
                    items[i] = user;
                    isUpdated = true;
                    break;
                }
            }
            if (isUpdated)
            {
                await WriteAsync(Constants.USERS_FILE, items);
            }
        }

        public async Task AddMessageAsync(IUser user, MessageItem message) {
            var roomId = message.IsSender ? message.ReceiveId : message.UserId;
            var items = await GetMessageDataAsync(roomId);
            items.Add(MessageFormatItem.WriteTo(message));
            await WriteAsync($"{Constants.MESSAGE_FOLDER}/{roomId}.db", items);
        }

        public async Task<T?> ReadAsync<T>(string fileName) 
        {
            var file = Storage.Combine(fileName);
            if (!File.Exists(file)) {
                return default;
            }
            return await Task.Factory.StartNew(() => 
            {
                using var fs = File.OpenRead(file);
                using var csDecrypt = Cipher.Decrypt(fs);
                var res = JsonSerializer.Deserialize(csDecrypt, typeof(T));
                if (res != null) {
                    return (T)res;
                }
                return default;
            });
        }

        public async Task WriteAsync<T>(string fileName, T data) 
        {
            var file = Storage.Combine(fileName);
            await Task.Factory.StartNew(() => 
            {
                using var fs = File.Create(file);
                using var csEncrypt = Cipher.Encrypt(fs);
                JsonSerializer.Serialize(csEncrypt, data);
            });
        }

        public async Task<string> ReadFileAsync(string fileName)
        {
            var file = Storage.Combine(fileName);
            if (!File.Exists(file)) {
                return string.Empty;
            }
            using var fs = File.OpenRead(file);
            using var csDecrypt = Cipher.Decrypt(fs);
            using var srDecrypt = new StreamReader(csDecrypt);
            return await srDecrypt.ReadToEndAsync();
        }

        public async Task WriteFileAsync(string fileName, string content)
        {
            var file = Storage.Combine(fileName);
            using var fs = File.Create(file);
            using var csEncrypt = Cipher.Encrypt(fs);
            using var swEncrypt = new StreamWriter(csEncrypt);
            await swEncrypt.WriteAsync(content);
        }

        public async Task ClearMessageAsync()
        {
            await Storage.DeleteFolderAsync(Constants.MESSAGE_FOLDER);
        }

        public async Task ResetAsync()
        {
            await ClearMessageAsync();
            await Storage.DeleteAsync(Constants.OPTION_FILE);
            await Storage.DeleteAsync(Constants.USERS_FILE);
        }
        public void Dispose() 
        {
        }

    }
}
