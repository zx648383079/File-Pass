using Microsoft.Data.Sqlite;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Repositories 
{
    public class SqlStore : IDatabaseStore {

        private readonly StorageRepository Storage;

        public string Password { get; private set; }

        private readonly string DbFile;
        private readonly string ConnectString;
        private SqliteConnection? connect;
        public SqliteConnection Connect 
        {
            get {
                if (connect == null)
                {
                    connect = new SqliteConnection(ConnectString);
                    connect.Open();
                }
                return connect;
            }
        }


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


        public Task<AppOption?> GetOptionAsync() {
            var query = Connect.CreateCommand();
            query.CommandText = @"SELECT 
                    Content
                    FROM Options WHERE Name=:name LIMIT 1";
            query.Parameters.AddWithValue(":name", "option");
            var str = query.ExecuteScalar()?.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return Task.FromResult((AppOption?)null);
            }
            var res = JsonSerializer.Deserialize(str, typeof(AppOption));
            return Task.FromResult((AppOption?)res);
        }

        public Task SaveOptionAsync(AppOption option) {
            var command = Connect.CreateCommand();
            command.CommandText =
                @"UPDATE Options 
                    SET Content=:content
                  WHERE Name=:name";
            command.Parameters.AddWithValue(":name", "option");
            command.Parameters.AddWithValue(":content", 
                JsonSerializer.Serialize(option, typeof(AppOption)));
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task<IList<UserItem>> GetUsersAsync() {
            IList<UserItem> items = new List<UserItem>();
            var command = Connect.CreateCommand();
            command.CommandText = @"SELECT 
                    Id,Name,Ip,Port,Avatar,MarkName,LastAt,LastMessage,EncryptType,EncryptRule
                    FROM Users ORDER BY UpdatedAt DESC";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new UserItem()
                    {
                        Id = reader.GetString(0),
                        Name = reader.GetString(1),
                        Ip = reader.GetString(2),
                        Port = reader.GetInt32(3),
                        Avatar = reader.GetString(4),
                        MarkName = reader.GetString(5),
                        LastAt = reader.GetDateTime(6),
                        LastMessage = reader.GetString(7),
                        EncryptType = reader.GetInt32(8),
                        EncryptRule = reader.GetString(9),
                    });
                }
            }
            return Task.FromResult(items);
        }

        public Task<IList<MessageItem>> GetMessagesAsync(IUser room, IUser user) {
            IList<MessageItem> items = new List<MessageItem>();
            var command = Connect.CreateCommand();
            command.CommandText = @"SELECT 
                    Id,UserId,ReceiveId,Type,Content,ExtraRule,Status,CreatedAt
                    FROM Messages WHERE (UserId=:user AND ReceiveId=:receive) 
                        OR (UserId=:receive AND ReceiveId=:user) ORDER BY CreatedAt ASC";
            command.Parameters.AddWithValue(":user", room.Id);
            command.Parameters.AddWithValue(":receive", user.Id);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var data = new MessageFormatItem()
                    {
                        Id = reader.GetString(0),
                        UserId = reader.GetString(1),
                        ReceiveId = reader.GetString(2),
                        Type = reader.GetInt32(3),
                        Content = reader.GetString(4),
                        ExtraRule = reader.GetString(5),
                        Status = reader.GetInt32(6),
                        CreatedAt = reader.GetDateTime(7),
                    };
                    var message = data.ReadFrom();
                    message.IsSender = room.Id != message.UserId;
                    items.Add(message);
                }
            }
            return Task.FromResult(items);
        }

        public Task RemoveUserAsync(IUser user) {
            var command = Connect.CreateCommand();
            command.CommandText =
                @"DELETE FROM Users WHERE Id=:id";
            command.Parameters.AddWithValue(":id", user.Id); ;
            command.ExecuteNonQuery();
            command = Connect.CreateCommand();
            command.CommandText =
                @"DELETE FROM Messages WHERE UserId=:id OR ReceiveId=:id";
            command.Parameters.AddWithValue(":id", user.Id); ;
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task AddUserAsync(IUser user) {
            var query = Connect.CreateCommand();
            query.CommandText = @"SELECT 
                    COUNT(Id)
                    FROM Users WHERE Id=:id LIMIT 1";
            query.Parameters.AddWithValue(":id", user.Id);
            var rows = Convert.ToInt32(query.ExecuteScalar());
            if (rows > 0)
            {
                return Task.CompletedTask;
            }
            var command = Connect.CreateCommand();
            command.CommandText =
                @"INSERT INTO Users (Id,Name,Ip,Port,Avatar,MarkName,EncryptType,EncryptRule,CreatedAt)
                  VALUES (:id,:name,:ip,:port,:avatar,:mark,:etype,:erule,:now)";
            command.Parameters.AddWithValue(":id", user.Id);
            command.Parameters.AddWithValue(":name", user.Name);
            command.Parameters.AddWithValue(":ip", user.Ip);
            command.Parameters.AddWithValue(":port", user.Port);
            command.Parameters.AddWithValue(":avatar", user.Avatar);
            if (user is UserItem info)
            {
                command.Parameters.AddWithValue(":mark", info.MarkName);
                command.Parameters.AddWithValue(":etype", info.EncryptType);
                command.Parameters.AddWithValue(":erule", info.EncryptRule);
            } else
            {
                command.Parameters.AddWithValue(":mark", user.Name);
                command.Parameters.AddWithValue(":etype", 0);
                command.Parameters.AddWithValue(":erule", "");
            }
            command.Parameters.AddWithValue(":now", DateTime.Now);
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(UserItem user) {
            var command = Connect.CreateCommand();
            command.CommandText =
                @"UPDATE  Users SET 
                    Name=:name,Ip=:ip,Port=:port,Avatar=:avatar,MarkName=:mark,EncryptType=:etype,EncryptRule=erule,LastAt=:at,LastMessage=:last
                 WHERE Id=:id";
            command.Parameters.AddWithValue(":id", user.Id);
            command.Parameters.AddWithValue(":name", user.Name);
            command.Parameters.AddWithValue(":ip", user.Ip);
            command.Parameters.AddWithValue(":port", user.Port);
            command.Parameters.AddWithValue(":avatar", user.Avatar);
            command.Parameters.AddWithValue(":mark", user.MarkName);
            command.Parameters.AddWithValue(":at", user.LastAt);
            command.Parameters.AddWithValue(":etype", user.EncryptType);
            command.Parameters.AddWithValue(":erule", user.EncryptRule);
            command.Parameters.AddWithValue(":last", user.LastMessage);
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task AddMessageAsync(IUser user, MessageItem message) {
            var data = MessageFormatItem.WriteTo(message);
            var command = Connect.CreateCommand();
            command.CommandText =
                @"INSERT INTO Messages (Id,UserId,ReceiveId,Type,Content,ExtraRule,Status,CreatedAt)
                  VALUES (:id,:user,:receive,:type,:content,:rule,:status,:now)";
            command.Parameters.AddWithValue(":id", message.Id);
            command.Parameters.AddWithValue(":user", message.UserId);
            command.Parameters.AddWithValue(":receive", message.ReceiveId);
            command.Parameters.AddWithValue(":status", message.IsSuccess);
            command.Parameters.AddWithValue(":now", message.CreatedAt);
            command.Parameters.AddWithValue(":type", data.Type);
            command.Parameters.AddWithValue(":content", data.Content);
            command.Parameters.AddWithValue(":rule", data.ExtraRule);
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task RemoveMessageAsync(MessageItem message)
        {
            var command = Connect.CreateCommand();
            if (string.IsNullOrEmpty(message.Id))
            {
                command.CommandText =
                                @"DELETE FROM Messages WHERE UserId=:user AND ReceiveId=:receive AND CreatedAt=:time LIMIT 1";
                command.Parameters.AddWithValue(":user", message.UserId);
                command.Parameters.AddWithValue(":receive", message.ReceiveId);
                command.Parameters.AddWithValue(":time", message.CreatedAt);
            } else
            {
                command.CommandText =
                                @"DELETE FROM Messages WHERE Id=:id LIMIT 1";
                command.Parameters.AddWithValue(":id", message.Id);
            }
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task ClearMessageAsync()
        {
            var command = Connect.CreateCommand();
            command.CommandText =
                                @"DELETE FROM Messages WHERE 1";
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public async Task ResetAsync()
        {
            await ClearMessageAsync();
            var command = Connect.CreateCommand();
            command.CommandText =
                                @"DELETE FROM Users WHERE 1";
            command.ExecuteNonQuery();
            command = Connect.CreateCommand();
            command.CommandText =
                                @"DELETE FROM Options WHERE 1";
            command.ExecuteNonQuery();
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
	""ReceiveId""	TEXT NOT NULL,
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
            var createTable = new SqliteCommand(sql, Connect);
            createTable.ExecuteReader();

            var command = Connect.CreateCommand();
            command.CommandText =
                @"INSERT INTO Options (Name,Content)
                  VALUES (:name,:content)";
            command.Parameters.AddWithValue(":name", "option");
            command.Parameters.AddWithValue(":content", "");
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public void Dispose() 
        {
            Connect?.Dispose();
        }

    }
}
