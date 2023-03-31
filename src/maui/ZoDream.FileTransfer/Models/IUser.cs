namespace ZoDream.FileTransfer.Models
{
    public interface IUser: IClientToken
    {
        public string Name { get; }
        public string Avatar { get; }
    }
}
