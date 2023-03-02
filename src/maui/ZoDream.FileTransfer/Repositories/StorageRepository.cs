using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Utils;

namespace ZoDream.FileTransfer.Repositories 
{
    public class StorageRepository
    {
        public StorageRepository(string baseFolder)
        {
            BaseFolder = baseFolder;
        }

        public string BaseFolder { get; private set; }

        public async Task InitializeAsync()
        {
            await MakeFolderAsync(Constants.FILE_CACHE_FOLDER);
        }

        public string Combine(string fileName)
        {
            return Path.Combine(BaseFolder, fileName);
        }

        public string Combine(string folder, string fileName)
        {
            return Path.Combine(BaseFolder, folder, fileName);
        }

        public async Task MakeFolderAsync(string name)
        {
            await Task.Factory.StartNew(() => {
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

        public async Task DeleteAsync(string fileName)
        {
            var file = Path.Combine(BaseFolder, fileName);
            await Task.Factory.StartNew(() => {
                if (!File.Exists(file))
                {
                    return;
                }
                File.Delete(file);
            });
        }

        public Stream CacheReader(string fileName)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            return File.OpenRead(file);
        }

        public Stream CacheWriter(string fileName)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            return File.Create(file);
        }

        public long CacheMergeFile(string destFile, params string[] partFiles)
        {
            using var writer = CacheWriter(destFile);
            foreach (var item in partFiles)
            {
                if (CacheExistFile(item))
                {
                    return 0L;
                }
                using var reader = CacheReader(item);
                reader.CopyTo(writer);
            }
            return writer.Length;
        }

        public bool CacheExistFile(string fileName)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            return File.Exists(file);
        }

        public string CacheFileMD5(string fileName)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            return Disk.GetMD5(file);
        }

        public void CacheMove(string fileName, string destFile)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            File.Move(file, destFile);
        }

        public void CacheRemove(params string[] fileNames)
        {
            foreach (var item in fileNames)
            {
                var file = Combine(Constants.FILE_CACHE_FOLDER, item);
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        public Task<long> GetSizeAsync(string fileName)
        {
            return Task.Factory.StartNew(() => {
                var info = new FileInfo(fileName);
                return info.Length;
            });
        }

        public Task<string> GetMD5Async(string fileName)
        {
            return Task.Factory.StartNew(() => {
                return Disk.GetMD5(fileName);
            });
        }

        
    }
}
