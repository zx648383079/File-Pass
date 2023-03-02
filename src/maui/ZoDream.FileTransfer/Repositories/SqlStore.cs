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

        private string DbFile;
        private string ConnectString;
        private SqliteConnection connect;
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


        public Task<AppOption> GetOptionAsync() {
            var query = Connect.CreateCommand();
            query.CommandText = @"SELECT 
                    Content
                    FROM Options WHERE Name=:name LIMIT 1";
            query.Parameters.AddWithValue(":name", "option");
            var str = query.ExecuteScalar().ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            var res = JsonSerializer.Deserialize(str, typeof(AppOption));
            if (res == null)
            {
                return null;
            }
            return Task.FromResult((AppOption)res);
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
                    Id,Name,Ip,Port,Avatar,MarkName,LastAt,LastMessage
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
                    var type = reader.GetInt32(3);
                    MessageItem message = type switch
                    {
                        1 => new ActionMessageItem()
                        {
                            Content = reader.GetString(4),
                        },
                        2 => new FileMessageItem()
                        {
                            FileName = reader.GetString(4),
                            Size = reader.GetInt32(5),
                        },
                        3 => new FolderMessageItem()
                        {
                            FolderName = reader.GetString(4),
                        },
                        4 => new SyncMessageItem()
                        {
                            FolderName = reader.GetString(4),
                        },
                        5 => new UserMessageItem()
                        {
                            Data = UserInfoItem.FromStr(reader.GetString(4)),
                        },
                        _ => new TextMessageItem()
                        {
                            Content = reader.GetString(4),
                        }
                    };
                    var userId = reader.GetString(1);
                    message.Id = reader.GetString(0);
                    message.UserId = userId;
                    message.ReceiveId = reader.GetString(2);
                    message.IsSender = room.Id != userId;
                    message.IsSuccess = reader.GetBoolean(6);
                    message.CreatedAt = reader.GetDateTime(7);
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
                @"INSERT INTO Users (Id,Name,Ip,Port,Avatar,MarkName,CreatedAt)
                  VALUES (:id,:name,:ip,:port,:avatar,:mark,:now)";
            command.Parameters.AddWithValue(":id", user.Id);
            command.Parameters.AddWithValue(":name", user.Name);
            command.Parameters.AddWithValue(":ip", user.Ip);
            command.Parameters.AddWithValue(":port", user.Port);
            command.Parameters.AddWithValue(":avatar", user.Avatar);
            if (user is UserItem info)
            {
                command.Parameters.AddWithValue(":mark", info.MarkName);
            } else
            {
                command.Parameters.AddWithValue(":mark", user.Name);
            }
            command.Parameters.AddWithValue(":now", DateTime.Now);
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(UserItem user) {
            var command = Connect.CreateCommand();
            command.CommandText =
                @"UPDATE  Users SET 
                    Name=:name,Ip=:ip,Port=:port,Avatar=:avatar,MarkName=:mark,LastAt=:at,LastMessage=:last
                 WHERE Id=:id";
            command.Parameters.AddWithValue(":id", user.Id);
            command.Parameters.AddWithValue(":name", user.Name);
            command.Parameters.AddWithValue(":ip", user.Ip);
            command.Parameters.AddWithValue(":port", user.Port);
            command.Parameters.AddWithValue(":avatar", user.Avatar);
            command.Parameters.AddWithValue(":mark", user.MarkName);
            command.Parameters.AddWithValue(":at", user.LastAt);
            command.Parameters.AddWithValue(":last", user.LastMessage);
            command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        public Task AddMessageAsync(IUser user, MessageItem message) {
            var command = Connect.CreateCommand();
            command.CommandText =
                @"INSERT INTO Messages (Id,UserId,ReceiveId,Type,Content,ExtraRule,Status,CreatedAt)
                  VALUES (:id,:user,:receive,:type,:content,:rule,:status,:now)";
            command.Parameters.AddWithValue(":id", message.Id);
            command.Parameters.AddWithValue(":user", message.UserId);
            command.Parameters.AddWithValue(":receive", message.ReceiveId);
            command.Parameters.AddWithValue(":status", message.IsSuccess);
            command.Parameters.AddWithValue(":now", message.CreatedAt);
            if (message is ActionMessageItem action)
            {
                command.Parameters.AddWithValue(":type", 1);
                command.Parameters.AddWithValue(":content", action.Content);
                command.Parameters.AddWithValue(":rule", "");
            } else if (message is TextMessageItem text)
            {
                command.Parameters.AddWithValue(":type", 0);
                command.Parameters.AddWithValue(":content", text.Content);
                command.Parameters.AddWithValue(":rule", "");
            }
            else if (message is SyncMessageItem sync)
            {
                command.Parameters.AddWithValue(":type", 4);
                command.Parameters.AddWithValue(":content", sync.FolderName);
                command.Parameters.AddWithValue(":rule", "");
            }
            else if (message is FolderMessageItem folder)
            {
                command.Parameters.AddWithValue(":type", 3);
                command.Parameters.AddWithValue(":content", folder.FolderName);
                command.Parameters.AddWithValue(":rule", "");
            }
            else if (message is FileMessageItem file)
            {
                command.Parameters.AddWithValue(":type", 2);
                command.Parameters.AddWithValue(":content", file.FileName);
                command.Parameters.AddWithValue(":rule", file.Size);
            }
            else if (message is UserMessageItem u)
            {
                command.Parameters.AddWithValue(":type", 5);
                command.Parameters.AddWithValue(":content", UserInfoItem.ToStr(u.Data));
                command.Parameters.AddWithValue(":rule", "");
            }
            else
            {
                return Task.CompletedTask;
            }
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
