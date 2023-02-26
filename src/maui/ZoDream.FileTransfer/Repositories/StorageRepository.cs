using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return File.OpenWrite(file);
        }

    }
}
