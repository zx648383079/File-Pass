namespace ZoDream.FileTransfer.Models
{
    public class MessageFormatItem
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string ReceiveId { get; set; }

        public int Type { get; set; }

        public string Content { get; set; }

        public string ExtraRule { get; set; } = string.Empty;

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public static MessageFormatItem WriteTo(MessageItem message)
        {
            var data = new MessageFormatItem()
            {
                Id = message.Id,
                ReceiveId = message.ReceiveId,
                UserId = message.UserId,
                CreatedAt = message.CreatedAt,
                Status = message.IsSuccess ? 1 : 0
            };
            if (message is ActionMessageItem action)
            {
                data.Type = 1;
                data.Content = action.Content;
            }
            else if (message is TextMessageItem text)
            {
                data.Type = 0;
                data.Content = text.Content;
            }
            //else if (message is SyncMessageItem sync)
            //{
            //    data.Type = 4;
            //    data.Content = sync.FolderName;
            //}
            else if (message is FolderMessageItem folder)
            {
                data.Type = 3;
                data.Content = folder.FolderName;
            }
            else if (message is FileMessageItem file)
            {
                data.Type = 2;
                data.Content = file.FileName;
                data.ExtraRule = file.Size.ToString();
            }
            else if (message is UserMessageItem u)
            {
                data.Type = 5;
                data.Content = UserInfoItem.ToStr(u.Data);
            }
            return data;
        }

        public static MessageItem ReadFrom(MessageFormatItem data)
        {
            MessageItem message = data.Type switch
            {
                1 => new ActionMessageItem()
                {
                    Content = data.Content,
                },
                2 => new FileMessageItem()
                {
                    FileName = data.Content,
                    Size = Convert.ToInt64(data.ExtraRule),
                },
                3 => new FolderMessageItem()
                {
                    FolderName = data.Content,
                },
                4 => new SyncMessageItem()
                {
                    FolderName = data.Content,
                },
                5 => new UserMessageItem()
                {
                    Data = UserInfoItem.FromStr(data.Content),
                },
                _ => new TextMessageItem()
                {
                    Content = data.Content,
                }
            };
            var userId = data.UserId;
            message.Id = data.Id;
            message.UserId = userId;
            message.ReceiveId = data.ReceiveId;
            message.IsSender = App.Repository.Option.Id == message.UserId;
            message.IsSuccess = data.Status > 0;
            message.CreatedAt = data.CreatedAt;
            return message;
        }

        public MessageItem ReadFrom()
        {
            return ReadFrom(this);
        }
    }
}
