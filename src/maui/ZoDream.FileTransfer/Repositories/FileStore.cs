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


        public Task<AppOption> GetOptionAsync() {
            return ReadAsync<AppOption>(Constants.OPTION_FILE);
        }

        public async Task SaveOptionAsync(AppOption option) {
            await WriteAsync(Constants.OPTION_FILE, option);
        }

        public async Task<IList<UserItem>> GetUsersAsync() {
            var items = await ReadAsync<IList<UserItem>>(Constants.USERS_FILE);
            return items is null ? new List<UserItem>() : items;
        }

        public async Task<IList<MessageItem>> GetMessagesAsync(IUser user) {
            return await ReadAsync<IList<MessageItem>>(
                $"{Constants.MESSAGE_FOLDER}/{user.Id}.db"
                );
        }

        public async Task RemoveUserAsync(IUser user) {
            await Storage.DeleteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db");
        }

        public async Task AddUserAsync(IUser user) {
            await WriteAsync(Constants.USERS_FILE, user);
        }

        public async Task UpdateUserAsync(UserItem user) {
            await WriteAsync(Constants.USERS_FILE, user);
        }

        public async Task AddMessageAsync(IUser user, MessageItem message) {
            await WriteAsync($"{Constants.MESSAGE_FOLDER}/{user.Id}.db", message);
        }

        public async Task<T> ReadAsync<T>(string fileName) 
        {
            var file = Storage.Combine(fileName);
            if (!File.Exists(file)) {
                return default;
            }
            return await Task.Factory.StartNew(() => 
            {
                using var fs = File.OpenRead(file);
                // Create an encryptor to perform the stream transform.
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
                using var fs = File.OpenWrite(file);

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
            using var fs = File.OpenWrite(file);

            // Create an encryptor to perform the stream transform.
            var encryptor = Cipher.CreateEncryptor(Cipher.Key, Cipher.IV);
            using var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            await swEncrypt.WriteAsync(content);
        }

        public void Dispose() 
        {
            Cipher?.Dispose();
        }

    }
}
