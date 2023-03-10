using System.Text.RegularExpressions;
using ZoDream.FileTransfer.Models;
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

        /// <summary>
        /// 删除文件夹下的文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task DeleteFolderAsync(string fileName)
        {
            var file = Path.Combine(BaseFolder, fileName);
            await Task.Factory.StartNew(() => {
                foreach (var item in Directory.GetFileSystemEntries(file))
                {
                    File.Delete(item);
                }
            });
        }

        public Stream CacheReader(string fileName)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            return File.OpenRead(file);
        }

        public Stream CacheWriter(string fileName, bool append = false)
        {
            var file = Combine(Constants.FILE_CACHE_FOLDER, fileName);
            if (append)
            {
                return File.OpenWrite(file);
            }
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
            if (!File.Exists(destFile))
            {
                File.Move(file, destFile);
                return;
            }
            var createdTime = File.GetCreationTime(destFile);
            File.Move(file, destFile, true);
            File.SetCreationTime(destFile, createdTime);
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

        public bool CheckFile(string fileName, string md5, bool overwrite = true)
        {
            if (!File.Exists(fileName))
            {
                return true;
            }
            if (!overwrite)
            {
                return false;
            }
            return Disk.GetMD5(fileName) != md5;
        }

        /// <summary>
        /// 获取系统盘符
        /// </summary>
        /// <returns></returns>
        public Task<List<FilePickerOption>> LoadDriverAsync()
        {
            return Task.Factory.StartNew(() => {
                var data = new List<FilePickerOption>();
#if WINDOWS
               var items = Environment.GetLogicalDrives();
                foreach (var item in items)
                {
                    data.Add(new FilePickerOption()
                    {
                        FileName = item,
                        Name = item[..(item.Length - 1)],
                        IsFolder = true,
                    });
                }
#endif

#if ANDROID
                var downloadFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                if (downloadFolder is not null)
                {
                    data.Add(new FilePickerOption()
                    {
                        FileName = downloadFolder.AbsolutePath,
                        Name = "下载",
                        IsFolder = true,
                    });
                }
#endif
                data.Add(new FilePickerOption()
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Name = "桌面",
                    IsFolder = true,
                });
                var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                data.Add(new FilePickerOption()
                {
                    FileName = docFolder,
                    Name = "文档",
                    IsFolder = true,
                });
                data.Add(new FilePickerOption()
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Name = "音乐",
                    IsFolder = true,
                });
                data.Add(new FilePickerOption()
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Name = "视频",
                    IsFolder = true,
                });
                data.Add(new FilePickerOption()
                {
                    FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Name = "图片",
                    IsFolder = true,
                });
#if WINDOWS
                data.Add(new FilePickerOption()
                {
                    FileName = Path.Combine(Path.GetDirectoryName(docFolder)!, "Downloads"),
                    Name = "下载",
                    IsFolder = true,
                });
#endif
                return data;
            });
        }

        public Task<List<FilePickerOption>> GetFilesAsync(string folder, bool isFolder, 
            Regex? filter)
        {
            return Task.Factory.StartNew(() => {
                var files = new List<FilePickerOption>();
                var folders = new List<FilePickerOption>();
                var dir = new DirectoryInfo(folder);
                var items = isFolder ? dir.GetDirectories() : dir.GetFileSystemInfos();
                foreach (var i in items)
                {
                    if (i is DirectoryInfo)     //判断是否文件夹
                    {
                        folders.Add(new FilePickerOption()
                        {
                            Name = i.Name,
                            IsFolder = true,
                            FileName = i.FullName,
                        });
                        continue;
                    }
                    if (filter is not null && !filter.IsMatch(i.Name))
                    {
                        continue;
                    }
                    files.Add(new FilePickerOption()
                    {
                        Name = i.Name,
                        IsFolder = false,
                        FileName = i.FullName,
                    });
                }
                folders.AddRange(files);
                return folders;
            });
        }
    }
}
