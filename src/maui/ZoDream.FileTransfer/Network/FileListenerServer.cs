using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public class FileListenerServer
    {

        private FileSystemWatcher Watcher;

        public FileListenerServer(string path)
        {
            Watcher = new FileSystemWatcher(path);
            Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName;
            Watcher.IncludeSubdirectories = true;
            Watcher.Created += Watcher_Created;
            Watcher.Renamed += Watcher_Renamed;
            Watcher.Deleted += Watcher_Deleted;
            Watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
