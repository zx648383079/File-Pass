using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Repositories
{
    internal static class Constants
    {
        public const string AES_IV = "zre.file";
        public const string SECURE_KEY = "zre.secure_key";

        public const int DEFAULT_PORT = 65530;
        public const int UDP_BUFFER_SIZE = 65536;
        public const string OPTION_FILE = "option.db";
        public const string USERS_FILE = "users.db";
        public const string MESSAGE_FOLDER = "Messages";
        public const string FILE_CACHE_FOLDER = "Caches";
        public const string SQL_DB = "zodream.db";
        public const bool UseSQL = false;


    }
}
