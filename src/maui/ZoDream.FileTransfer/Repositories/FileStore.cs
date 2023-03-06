using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Repositories
{
    public class FileStore: IDatabaseStore
    {
        private readonly StorageRepository Storage;

        public string Password { get; private set; }

        private readonly Aes Cipher;

        public FileStore(StorageRepository storage, string password) 
        {
            Storage = storage;
            Password = password;
            try {
                Cipher = Aes.Create();
                Cipher.Key = Encoding.UTF8.GetBytes(Password);
                Cipher.IV = VerifyIV(Encoding.UTF8.GetBytes(Constants.AES_IV), 16);
            }
            catch (Exception) 
            {

            }
        }

        private byte[] VerifyIV(byte[] iv, int size) {
            if (iv.Length == size) {
                return iv;
            }
            var items = new byte[size];
            for (int i = 0; i < size; i++) {
                if (iv.Length > i) {
                    items[i] = iv[i];
                }
                else {
                    items[i] = (byte)i;
                }
            }
            return items;
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
            var items = await ReadAsync<IList<MessageItem>>(
                $"{Constants.MESSAGE_FOLDER}/{userId}.db"
                );
            return items ?? new List<MessageItem>();
        }

        public async Task RemoveMessageAsync(MessageItem message)
        {
            var roomId = message.IsSender ? message.ReceiveId : message.UserId;
            var items = await GetMessagesAsync(roomId);
            var isUpdated = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (
                    (string.IsNullOrEmpty(message.Id) 
                    && items[i].CreatedAt == message.CreatedAt && message.IsSender == items[i].IsSender) 
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
            var items = await GetMessagesAsync(roomId);
            items.Add(message);
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
                var descriptor = Cipher.CreateDecryptor(Cipher.Key, Cipher.IV);
                using var csDecrypt = new CryptoStream(fs, descriptor, CryptoStreamMode.Read);
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

                // Create an encryptor to perform the stream transform.
                var encryptor = Cipher.CreateEncryptor(Cipher.Key, Cipher.IV);
                using var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
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
            // Create an encryptor to perform the stream transform.
            var descriptor = Cipher.CreateDecryptor(Cipher.Key, Cipher.IV);
            using var csDecrypt = new CryptoStream(fs, descriptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return await srDecrypt.ReadToEndAsync();
        }

        public async Task WriteFileAsync(string fileName, string content)
        {
            var file = Storage.Combine(fileName);
            using var fs = File.Create(file);

            // Create an encryptor to perform the stream transform.
            var encryptor = Cipher.CreateEncryptor(Cipher.Key, Cipher.IV);
            using var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
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
            Cipher?.Dispose();
        }

    }
}
