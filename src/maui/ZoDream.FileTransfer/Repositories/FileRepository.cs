using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace ZoDream.FileTransfer.Repositories
{
    internal class FileRepository: IDisposable
    {
        public FileRepository(
            string baseFolder,
            Aes aes
            )
        {
            BaseFolder = baseFolder;
            Aes = aes;
        }

        public FileRepository(
            string baseFolder,
            byte[] aesKey,
            byte[] aesIv
            )
        {
            BaseFolder = baseFolder;
            Aes = Aes.Create();
            Aes.Key = aesKey;
            Aes.IV = aesIv;
        }

        public string BaseFolder { get; private set; }

        public Aes Aes { get; private set; }

        public async Task MakeFolderAsync(string name)
        {
            await Task.Factory.StartNew(() =>
            {
                MakeFolder(name);
            });
        }

        private string MakeFolder(string name)
        {
            var file = Path.Combine(BaseFolder, name);
            if (Directory.Exists(file))
            {
                return file;
            }
            Directory.CreateDirectory(file);
            return file;
        }

        public Stream CacheReader(string fileName)
        {
            var folder = MakeFolder(Constants.FILE_CACHE_FOLDER);
            var file = Path.Combine(folder, fileName);
            return File.OpenRead(file);
        }

        public Stream CacheWriter(string fileName)
        {
            var folder = MakeFolder(Constants.FILE_CACHE_FOLDER);
            var file = Path.Combine(folder, fileName);
            return File.OpenWrite(file);
        }

        public async Task DeleteAsync(string fileName)
        {
            var file = Path.Combine(BaseFolder, fileName);
            await Task.Factory.StartNew(() =>
            {
                if (!File.Exists(file))
                {
                    return;
                }
                File.Delete(file);
            });
        }

        public async Task<T> ReadAsync<T>(string fileName)
        {
            var file = Path.Combine(BaseFolder, fileName);
            if (!File.Exists(file))
            {
                return default;
            }
            return await Task.Factory.StartNew(() =>
            {
                using var fs = File.OpenRead(file);
                // Create an encryptor to perform the stream transform.
                var descriptor = Aes.CreateDecryptor(Aes.Key, Aes.IV);
                using var csDecrypt = new CryptoStream(fs, descriptor, CryptoStreamMode.Read);
                var res = JsonSerializer.Deserialize(csDecrypt, typeof(T));
                if (res != null)
                {
                    return (T)res;
                }
                return default;
            });
        }

        public async Task WriteAsync<T>(string fileName, T data)
        {
            var file = Path.Combine(BaseFolder, fileName);
            await Task.Factory.StartNew(() =>
            {
                using var fs = File.OpenWrite(file);

                // Create an encryptor to perform the stream transform.
                var encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV);
                using var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
                JsonSerializer.Serialize(csEncrypt, data);
            });
        }

        public async Task<string> ReadFileAsync(string fileName)
        {
            var file = Path.Combine(BaseFolder, fileName);
            if (!File.Exists(file))
            {
                return string.Empty;
            }
            using var fs = File.OpenRead(file);
            // Create an encryptor to perform the stream transform.
            var descriptor = Aes.CreateDecryptor(Aes.Key, Aes.IV);
            using var csDecrypt = new CryptoStream(fs, descriptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return await srDecrypt.ReadToEndAsync();
        }

        public async Task WriteFileAsync(string fileName, string content)
        {
            var file = Path.Combine(BaseFolder, fileName);
            using var fs = File.OpenWrite(file);

            // Create an encryptor to perform the stream transform.
            var encryptor = Aes.CreateEncryptor(Aes.Key, Aes.IV);
            using var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            await swEncrypt.WriteAsync(content);
        }

        public void Dispose()
        {
            Aes.Dispose();
        }

    }
}
