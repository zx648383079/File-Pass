using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Repositories 
{
    public class SqlStore : IDatabaseStore {

        private readonly StorageRepository Storage;

        public string Password { get; private set; }

        private string DbFile;
        private string ConnectString;


        public SqlStore(StorageRepository storage, string password)
        {
            Storage = storage;
            Password = password;
            DbFile = Storage.Combine(Constants.SQL_DB);
            ConnectString = new SqliteConnectionStringBuilder() {
                DataSource = DbFile,
                Password = Password
            }.ToString();

        }


        public Task<AppOption> GetOptionAsync() {
            throw new NotImplementedException();
        }

        public Task SaveOptionAsync(AppOption option) {
            throw new NotImplementedException();
        }

        public Task<IList<UserItem>> GetUsersAsync() {
            throw new NotImplementedException();
        }

        public Task<IList<MessageItem>> GetMessagesAsync(IUser user) {
            throw new NotImplementedException();
        }

        public Task RemoveUserAsync(IUser user) {
            throw new NotImplementedException();
        }

        public Task AddUserAsync(IUser user) {
            throw new NotImplementedException();
        }

        public Task UpdateUserAsync(UserItem user) {
            throw new NotImplementedException();
        }

        public Task AddMessageAsync(IUser user, MessageItem message) {
            throw new NotImplementedException();
        }

        public Task InitializeAsync() 
        {
            var overwrite = true;
            var exist = File.Exists(DbFile);
            if (exist && !overwrite)
            {
                return Task.FromResult(true);
            }
            using (var fs = new FileStream(DbFile, FileMode.Create)) {}
            using var db = new SqliteConnection(ConnectString);
            db.Open();
            var sql = @"
CREATE TABLE ""Users"" (
	""Id""	TEXT NOT NULL UNIQUE,
	""Name""	TEXT NOT NULL,
	""Avatar""	TEXT NOT NULL,
	""MarkName""	TEXT,
	""Ip""	TEXT NOT NULL,
	""Port""	INTEGER NOT NULL,
	""LastAt""	TEXT,
	""LastMessage""	TEXT,
	""Disabled""	INTEGER DEFAULT 0,
	""EncryptType""	INTEGER DEFAULT 0,
	""EncryptRule""	TEXT,
	""CreatedAt""	TEXT NOT NULL,
	PRIMARY KEY(""Id"")
);
CREATE TABLE ""Messages"" (
	""Id""	INTEGER NOT NULL UNIQUE,
	""UserId""	TEXT NOT NULL,
	""Type""	INTEGER NOT NULL DEFAULT 0,
	""Content""	TEXT NOT NULL,
	""ExtraRule""	TEXT,
	""Status""	INTEGER NOT NULL DEFAULT 0,
	""CreatedAt""	TEXT NOT NULL,
	PRIMARY KEY(""Id"" AUTOINCREMENT)
);
CREATE TABLE ""Options"" (
	""Name""	TEXT NOT NULL UNIQUE,
	""Content""	TEXT NOT NULL,
	PRIMARY KEY(""Name"")
);
";
            var createTable = new SqliteCommand(sql, db);
            createTable.ExecuteReader();
            return Task.FromResult(true);
        }

        public void Dispose() 
        {
            
        }

    }
}
