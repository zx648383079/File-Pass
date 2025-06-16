using ZoDream.Shared.Interfaces;

namespace ZoDream.Shared.Model
{
    public class User: IUser
    {
        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Avatar { get; private set; } = string.Empty;

        public User()
        {
        }

        public User(int id, string name, string avatar)
        {
            Id = id;
            Name = name;
            Avatar = avatar;
        }
    }
}
